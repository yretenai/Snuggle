using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using DragonLib;
using Mono.Cecil;
using Serilog;
using Snuggle.Core.Implementations;
using Snuggle.Core.Interfaces;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models;
using Snuggle.Core.Models.Bundle;
using Snuggle.Core.Models.Serialization;
using Snuggle.Core.Options;

namespace Snuggle.Core;

public class AssetCollection : IDisposable {
    public List<IAssetBundle> Bundles { get; } = new();
    public AssemblyResolver Assemblies { get; set; } = new();
    public ConcurrentDictionary<string, ObjectNode> Types { get; } = new();
    public ConcurrentDictionary<string, SerializedFile> Files { get; } = new(StringComparer.InvariantCultureIgnoreCase);
    public ConcurrentDictionary<string, (object Tag, IFileHandler Handler)> Resources { get; } = new(StringComparer.InvariantCultureIgnoreCase);
    public List<GameObject> GameObjectTree { get; set; } = new();
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
        GameObjectTree.Clear();
        Types.Clear();
        Collect();
    }

    public void RebuildBundles(string outputDir, BundleSerializationOptions bundleOptions, SnuggleCoreOptions options, CancellationToken cancellationToken) {
        outputDir.EnsureDirectoryExists();
        options.Reporter?.SetProgressMax(Bundles.Count);
        try {
            for (var index = 0; index < Bundles.Count; index++) {
                if (cancellationToken.IsCancellationRequested) {
                    break;
                }

                var bundle = Bundles[index];
                var name = IFileHandler.UnpackTagToNameWithoutExtension(bundle.Tag);
                options.Reporter?.SetProgress(index);
                options.Reporter?.SetStatus($"Rebuilding {name}");
                using var fs = new FileStream(Path.Combine(outputDir, name + ".bundle"), FileMode.OpenOrCreate, FileAccess.Write);
                IAssetBundle.RebuildBundle(bundle, bundle.GetBlocks(), bundleOptions, fs);
            }
        } catch (Exception e) {
            options.Reporter?.Reset();
            options.Reporter?.SetStatus("Rebuilding failed!");
            Log.Error(e, "Rebuilding failed!");
        }
        
        options.Reporter?.Reset();
    }

    public void LoadBundle(IAssetBundle bundle) {
        var handler = new BundleStreamHandler(bundle);
        foreach (var block in bundle.GetBlocks()) {
            if (((UnityBundleBlockFlags) block.Flags).HasFlag(UnityBundleBlockFlags.SerializedFile)) {
                try {
                    LoadSerializedFile(bundle.OpenFile(block), block, handler, bundle.Options, false, bundle.Version);
                } catch (Exception e) {
                    Log.Error(e, "Failure decoding bundle");
                }
            } else {
                var ext = Path.GetExtension(block.Path).ToLower();
                switch (ext) {
                    case ".ress":
                    case ".resource":
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
                try {
                    LoadBundle(bundle);
                } catch (Exception e) {
                    Log.Error(e, "Failure decoding bundle");
                }
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

            foreach (var objectInfo in file.ObjectInfos) {
                options.Reporter?.SetStatus($"Processing {objectInfo.PathId} ({objectInfo.ClassId:G})");
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

    public void LoadSplitFile(string split0Path, SnuggleCoreOptions options, string extTemplate = ".split{0}") {
        var i = 0;
        var streams = new List<Stream>();
        while (File.Exists(split0Path + string.Format(extTemplate, i))) {
            streams.Add(File.OpenRead(split0Path + string.Format(extTemplate, i++)));
        }

        LoadSplitFile(streams, options, split0Path);
    }

    public void LoadSplitFile(List<Stream> streams, SnuggleCoreOptions options, string hintTag = "splitFile", bool leaveOpen = false) {
        var memory = new MemoryStream();
        memory.SetLength(streams.Sum(x => x.Length - x.Position));
        memory.Seek(0, SeekOrigin.Begin);

        foreach (var stream in streams) {
            stream.CopyTo(memory);
            if (!leaveOpen) {
                stream.Close();
                stream.Dispose();
            }
        }

        memory.Seek(0, SeekOrigin.Begin);
        LoadFile(memory, hintTag, new MemoryStreamHandler(memory), options, leaveOpen: true);
    }

    public void LoadFile(string path, SnuggleCoreOptions options) => LoadFile(File.OpenRead(path), path, MultiStreamHandler.FileInstance.Value, options);

    private void LoadFile(Stream dataStream, object tag, IFileHandler handler, SnuggleCoreOptions options, int align = 1, bool leaveOpen = false) {
        Log.Information("Attempting to load {Tag}", tag);
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
        } catch (Exception e) {
            Log.Error(e, "Unexpected error while loading file");
        } finally {
            if (!leaveOpen) {
                dataStream.Close();
            }
        }
    }

    public static void Collect() {
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    public void ClearTypeTrees() {
        foreach (var (_, file) in Files) {
            file.Types = Array.Empty<UnitySerializedType>();
            file.ReferenceTypes = Array.Empty<UnitySerializedType>();
        }

        Types.Clear();
    }

    public void ClearCaches() {
        Types.Clear();
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
            Log.Error(e, "Unexpected error while assembly {Assembly} file", assemblyLocation);
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
            file.FindResources();
        }
    }

    public void CacheGameObjectClassIds() {
        foreach (var serializedObject in Files.SelectMany(pair => pair.Value.GetAllObjects().OfType<GameObject>())) {
            serializedObject.CacheClassIds();
        }
    }

    public void BuildGraph() {
        foreach (var (_, file) in Files) {
            foreach (var info in file.ObjectInfos) {
                if (!info.ClassId.Equals(UnityClassId.GameObject)) {
                    continue;
                }

                if (file.GetObject(info.PathId) is not GameObject gameObject) {
                    continue;
                }

                if (!gameObject.Parent.IsNull) {
                    continue;
                }

                GameObjectTree.Add(gameObject);
            }
        }
    }
}
