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
    public List<IVirtualStorage> VFSes { get; } = new();
    public AssemblyResolver Assemblies { get; set; } = new();
    public ConcurrentDictionary<string, ObjectNode> Types { get; } = new();
    public ConcurrentDictionary<string, SerializedFile> Files { get; } = new(StringComparer.InvariantCultureIgnoreCase);
    public ConcurrentDictionary<string, (object Tag, IFileHandler Handler)> Resources { get; } = new(StringComparer.InvariantCultureIgnoreCase);
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

        foreach (var virtualStorage in VFSes) {
            try {
                (virtualStorage as IDisposable)?.Dispose();
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

    public void LoadBundle(string path, SnuggleCoreOptions options, bool leaveOpen = false) {
        var isSplit = Path.GetExtension(path) == ".split0";
        LoadBundle(isSplit ? new SplitFileStream(path) : new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), path, isSplit ? OffsetStreamHandler.SplitInstance.Value : OffsetStreamHandler.FileInstance.Value, options, leaveOpen);
    }

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

    public void LoadBundleSequence(string path, SnuggleCoreOptions options, int align = 1) {
        var isSplit = Path.GetExtension(path) == ".split0";
        LoadBundleSequence(isSplit ? new SplitFileStream(path) : new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), path, isSplit ? OffsetStreamHandler.SplitInstance.Value : OffsetStreamHandler.FileInstance.Value, options, align);
    }

    public void LoadSerializedFile(Stream dataStream, object tag, IFileHandler handler, SnuggleCoreOptions options, bool leaveOpen = false, UnityVersion? fallbackVersion = null) {
        try {
            var path = IFileHandler.UnpackTagToName(tag);
            if (string.IsNullOrEmpty(path)) {
                return;
            }

            if (Files.ContainsKey(path)) {
                return;
            }

            var file = new SerializedFile(dataStream, tag, handler, options, true) { Assets = this, Name = path };
            if (file.Version == UnityVersion.MinValue && fallbackVersion != null && fallbackVersion != UnityVersion.MinValue) {
                file.Version = fallbackVersion.Value;
            }

            foreach (var objectInfo in file.ObjectInfos) {
                options.Reporter?.SetSubStatus($"Processing {objectInfo.PathId} ({objectInfo.ClassId:G})");
                file.PreloadObject(objectInfo, options, dataStream);
            }

            Files[path] = file;
        } finally {
            if (!leaveOpen) {
                dataStream.Dispose();
            }
        }
    }

    public void LoadSerializedFile(string path, SnuggleCoreOptions options) {
        var isSplit = Path.GetExtension(path) == ".split0";
        LoadSerializedFile(isSplit ? new SplitFileStream(path) : new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), path, isSplit ? OffsetStreamHandler.SplitInstance.Value : OffsetStreamHandler.FileInstance.Value, options);
    }

    public void LoadVFS(Stream dataStream, object tag, IFileHandler handler, SnuggleCoreOptions options, bool leaveOpen = false) {
        var vfs = IVirtualStorage.Init(dataStream, tag, handler, options, leaveOpen);
        var vfsHandler = new VFSStreamHandler(vfs);
        VFSes.Add(vfs);

        foreach (var file in vfs.Entries) {
            using var stream = vfs.Open(file, dataStream, true);
            LoadFile(stream, file, vfsHandler, options, leaveOpen: leaveOpen);
        }
    }

    public void LoadFile(string path, SnuggleCoreOptions options) {
        var isSplit = Path.GetExtension(path) == ".split0";
        LoadFile(isSplit ? new SplitFileStream(path) : new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), path, isSplit ? OffsetStreamHandler.SplitInstance.Value : OffsetStreamHandler.FileInstance.Value, options);
    }

    private void LoadFile(Stream dataStream, object tag, IFileHandler handler, SnuggleCoreOptions options, int align = 1, bool leaveOpen = false) {
        var path = IFileHandler.UnpackTagToString(tag);
        Log.Information("Attempting to load {Tag}", path ?? tag);
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
            } else if (IVirtualStorage.IsVFSFile(tag, dataStream, options)) {
                LoadVFS(dataStream, tag, handler, options, leaveOpen);
            } else {
                if (string.IsNullOrEmpty(path)) {
                    return;
                }

                path = Path.GetFileName(path);
                var ext = Path.GetExtension(path).ToLower() ?? "";
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

    public List<GameObject> BuildGraph() {
        var tree = new List<GameObject>();
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

                tree.Add(gameObject);
            }
        }

        return tree;
    }
}
