using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Mono.Cecil;
using Snuggle.Core.Implementations;
using Snuggle.Core.Interfaces;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models.Bundle;
using Snuggle.Core.Options;

namespace Snuggle.Core;

[PublicAPI]
public class AssetCollection : IDisposable {
    public List<Bundle> Bundles { get; } = new();
    public AssemblyResolver Assemblies { get; set; } = new();
    public Dictionary<string, ObjectNode> Types { get; } = new();
    public Dictionary<string, SerializedFile> Files { get; } = new(StringComparer.InvariantCultureIgnoreCase);
    public Dictionary<string, (object Tag, IFileHandler Handler)> Resources { get; } = new(StringComparer.InvariantCultureIgnoreCase);
    public PlayerSettings? PlayerSettings { get; internal set; }

    public void Dispose() {
        Reset();
        Assemblies.Dispose();
        GC.SuppressFinalize(this);
    }

    public void Reset() {
        foreach (var bundle in Bundles) {
            try {
                bundle.Dispose();
            } catch {
                // ignored 
            }
        }

        foreach (var (_, file) in Files) {
            file.Free();
        }

        PlayerSettings = null;

        Bundles.Clear();
        Assemblies.Clear();
        Files.Clear();
        Resources.Clear();
        Collect();
    }

    public void LoadBundle(Bundle bundle) {
        var handler = new BundleStreamHandler(bundle);
        foreach (var block in bundle.Container.Blocks) {
            if (block.Flags.HasFlag(UnityBundleBlockFlags.SerializedFile)) {
                LoadSerializedFile(bundle.OpenFile(block), block, handler, bundle.Options, false, bundle.Header.Version);
            } else {
                var ext = Path.GetExtension(block.Path)[1..].ToLower();
                switch (ext) {
                    case "ress":
                    case "resource":
                        Resources[block.Path] = (block, handler);
                        break;
                    default:
                        // ??
                        continue;
                }
            }
        }

        Bundles.Add(bundle);
    }

    public void LoadBundle(Stream dataStream, object tag, IFileHandler handler, SnuggleCoreOptions options, bool leaveOpen = false) => LoadBundle(new Bundle(dataStream, tag, handler, options, leaveOpen));

    public void LoadBundle(string path, SnuggleCoreOptions options, bool leaveOpen = false) => LoadBundle(File.OpenRead(path), path, FileStreamHandler.Instance.Value, options, leaveOpen);

    public void LoadBundleSequence(Stream dataStream, object tag, IFileHandler handler, SnuggleCoreOptions options, int align = 1, bool leaveOpen = false) {
        try {
            var bundles = Bundle.OpenBundleSequence(dataStream, tag, handler, options, align, leaveOpen);
            foreach (var bundle in bundles) {
                LoadBundle(bundle);
            }
        } finally {
            if (!leaveOpen) {
                dataStream.Close();
            }
        }
    }

    public void LoadBundleSequence(string path, SnuggleCoreOptions options, int align = 1) => LoadBundleSequence(File.OpenRead(path), path, MultiStreamHandler.FileInstance.Value, options, align);

    public void LoadSerializedFile(Stream dataStream, object tag, IFileHandler handler, SnuggleCoreOptions options, bool leaveOpen = false, UnityVersion? fallbackVersion = null) {
        try {
            var path = tag switch {
                UnityBundleBlock block => block.Path,
                string str => Path.GetFileName(str),
                _ => throw new NotSupportedException($"{tag.GetType().FullName} is not supported"),
            };

            if (Files.ContainsKey(path)) {
                return;
            }

            var file = new SerializedFile(dataStream, tag, handler, options, true) { Assets = this, Name = path };
            if (file.Version == UnityVersion.MinValue && fallbackVersion != null && fallbackVersion != UnityVersion.MinValue) {
                file.Version = fallbackVersion.Value;
            }

            foreach (var (pathId, objectInfo) in file.ObjectInfos) {
                options.Reporter?.SetStatus($"Processing {pathId} ({objectInfo.ClassId:G})");
                file.PreloadObject(objectInfo, options, dataStream);
            }

            Files[path] = file;
        } finally {
            if (!leaveOpen) {
                dataStream.Dispose();
            }
        }
    }

    public void LoadSerializedFile(string path, SnuggleCoreOptions options) => LoadSerializedFile(File.OpenRead(path), path, FileStreamHandler.Instance.Value, options);

    public void LoadFile(string path, SnuggleCoreOptions options) => LoadFile(File.OpenRead(path), path, MultiStreamHandler.FileInstance.Value, options);

    private void LoadFile(Stream dataStream, object tag, IFileHandler handler, SnuggleCoreOptions options, int align = 1, bool leaveOpen = false) {
        options.Logger.Info($"Attempting to load {tag}");
        if (dataStream.Length == 0) {
            return;
        }
        
        try {
            if (Bundle.IsBundleFile(dataStream)) {
                LoadBundleSequence(dataStream, tag, handler, options, align, leaveOpen);
            } else if (dataStream is FileStream fs && IsAssembly(dataStream)) {
                LoadAssembly(dataStream, Path.GetDirectoryName(fs.Name) ?? "./", options, leaveOpen);
            } else if (SerializedFile.IsSerializedFile(dataStream)) {
                LoadSerializedFile(dataStream, tag, handler, options, leaveOpen);
            } else {
                if (tag is not string path) {
                    throw new NotSupportedException($"{tag.GetType().FullName} is not supported");
                }

                path = Path.GetFileName(path);
                var ext = Path.GetExtension(path).ToLower();
                switch (ext) {
                    case ".ress":
                    case ".resource":
                        Resources[path] = (tag, handler);
                        break;
                    default:
                        return;
                }
            }
        } finally {
            if (!leaveOpen) {
                dataStream.Close();
            }
        }
    }

    public static void Collect() {
        Utils.ClearPool();
        GC.Collect();
    }

    public void LoadAssembly(Stream dataStream, string assemblyLocation, SnuggleCoreOptions options, bool leaveOpen = false) {
        try {
            Assemblies.RemoveSearchDirectory(assemblyLocation);
            Assemblies.AddSearchDirectory(assemblyLocation);
            Assemblies.RegisterAssembly(AssemblyDefinition.ReadAssembly(new OffsetStream(dataStream, null, null, leaveOpen), new ReaderParameters(ReadingMode.Immediate) { InMemory = true, AssemblyResolver = Assemblies }));
            if (!leaveOpen) {
                dataStream.Dispose();
            }
        } catch (Exception e) {
            options.Logger.Error("Assets", $"Failed to load assembly from {assemblyLocation}", e);
        } finally {
            if (!leaveOpen) {
                dataStream.Close();
            }
        }
    }

    public static bool IsAssembly(Stream stream) {
        var pos = stream.Position;
        try {
            using var reader = new BiEndianBinaryReader(stream, false, true);
            if (reader.ReadUInt16() != 0x5A4D) { // no MZ header
                return false;
            }

            stream.Position = pos + 0x3C;
            var peOffset = reader.ReadInt32();
            if (peOffset > stream.Length) {
                return false;
            }

            stream.Position = pos + peOffset;

            if (reader.ReadUInt16() != 0x4550) { // no PE header
                return false;
            }

            stream.Position = pos;
            
            using var module = ModuleDefinition.ReadModule(new OffsetStream(stream, leaveOpen: true), new ReaderParameters(ReadingMode.Deferred) { InMemory = true });
            return module.Kind == ModuleKind.Dll && module.HasTypes;
        } catch {
            return false;
        } finally {
            stream.Seek(pos, SeekOrigin.Begin);
        }
    }

    public bool TryOpenResource(string path, [MaybeNullWhen(false)] out Stream stream) {
        if (!Resources.TryGetValue(path, out var resourceLoader)) {
            if (!Resources.TryGetValue(Path.GetFileName(path), out resourceLoader)) {
                stream = null;
                return false;
            }
        }

        var (tag, handler) = resourceLoader;

        stream = handler.OpenFile(tag);
        return true;
    }

    public void FindResources() {
        foreach (var file in Files.Values) {
            file.FindResources(default);
        }
    }

    public void CacheGameObjectClassIds() {
        foreach (var serializedObject in Files.SelectMany(pair => pair.Value.GetAllObjects().OfType<GameObject>())) {
            serializedObject.CacheClassIds();
        }
    }
}
