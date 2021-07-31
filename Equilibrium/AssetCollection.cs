using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Meta.Interfaces;
using Equilibrium.Meta.Options;
using Equilibrium.Models;
using Equilibrium.Models.Bundle;
using JetBrains.Annotations;
using Mono.Cecil;

namespace Equilibrium {
    [PublicAPI]
    public class AssetCollection : IDisposable {
        public List<Bundle> Bundles { get; } = new();
        public AssemblyResolver Assemblies { get; set; } = new();
        public Dictionary<string, ObjectNode> Types { get; } = new();
        public Dictionary<string, SerializedFile> Files { get; } = new(StringComparer.InvariantCultureIgnoreCase);
        public Dictionary<string, (object Tag, IFileHandler Handler)> Resources { get; } = new(StringComparer.InvariantCultureIgnoreCase);

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

            Bundles.Clear();
            Assemblies.Clear();
            Files.Clear();
            Resources.Clear();

            GC.Collect();
        }

        public void LoadBundle(Bundle bundle) {
            var handler = new BundleStreamHandler(bundle);
            foreach (var block in bundle.Container.Blocks) {
                if (block.Flags.HasFlag(UnityBundleBlockFlags.SerializedFile)) {
                    LoadSerializedFile(bundle.OpenFile(block), block, handler, false, bundle.Header.Version, bundle.Options);
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

        public void LoadBundle(Stream dataStream, object tag, IFileHandler handler, EquilibriumOptions? options, bool leaveOpen = false) => LoadBundle(new Bundle(dataStream, tag, handler, options ?? EquilibriumOptions.Default, leaveOpen));

        public void LoadBundle(string path, EquilibriumOptions? options, bool leaveOpen = false) => LoadBundle(File.OpenRead(path), path, FileStreamHandler.Instance.Value, options, leaveOpen);

        public void LoadBundleSequence(Stream dataStream, object tag, IFileHandler handler, EquilibriumOptions? options = null, int align = 1, bool leaveOpen = false) {
            var bundles = Bundle.OpenBundleSequence(dataStream, tag, handler, align, options, leaveOpen);
            foreach (var bundle in bundles) {
                LoadBundle(bundle);
            }
        }

        public void LoadBundleSequence(string path, EquilibriumOptions? options = null, int align = 1) => LoadBundleSequence(File.OpenRead(path), path, MultiStreamHandler.FileInstance.Value, options, align);

        public void LoadSerializedFile(Stream dataStream, object tag, IFileHandler handler, bool leaveOpen = false, UnityVersion? fallbackVersion = null, EquilibriumOptions? options = null) {
            var path = tag switch {
                UnityBundleBlock block => block.Path,
                string str => Path.GetFileName(str),
                _ => throw new InvalidOperationException(),
            };

            if (Files.ContainsKey(path)) {
                if (!leaveOpen) {
                    dataStream.Dispose();
                }

                return;
            }

            var file = new SerializedFile(dataStream, tag, handler, options ?? EquilibriumOptions.Default, true) { Assets = this, Name = path };
            if (file.Version == UnityVersion.MinValue &&
                fallbackVersion != null &&
                fallbackVersion != UnityVersion.MinValue) {
                file.Version = fallbackVersion.Value;
            }

            foreach (var (pathId, objectInfo) in file.ObjectInfos) {
                try {
                    options?.Reporter?.SetStatus($"Processing {pathId} ({objectInfo.ClassId:G})");
                    file.Objects[pathId] = ObjectFactory.GetInstance(dataStream, objectInfo, file);
                } catch (Exception e) {
                    Debug.WriteLine($"Failed to decode {pathId} (type {objectInfo.ClassId}) on file {file.Name}.");
                    Debug.WriteLine(e);
                    file.Objects[pathId] = ObjectFactory.GetInstance(dataStream, objectInfo, file, UnityClassId.Object);
                }
            }

            Files[path] = file;

            if (!leaveOpen) {
                dataStream.Dispose();
            }
        }

        public void LoadSerializedFile(string path, EquilibriumOptions? options) => LoadSerializedFile(File.OpenRead(path), path, FileStreamHandler.Instance.Value, false, null, options);

        public void LoadFile(string path, EquilibriumOptions? options = null) => LoadFile(File.OpenRead(path), path, MultiStreamHandler.FileInstance.Value, options);

        private void LoadFile(Stream dataStream, object tag, IFileHandler handler, EquilibriumOptions? options = null, int align = 1, bool leaveOpen = false) {
            if (SerializedFile.IsSerializedFile(dataStream)) {
                LoadSerializedFile(dataStream, tag, handler, leaveOpen, null, options);
            } else if (Bundle.IsBundleFile(dataStream)) {
                LoadBundleSequence(dataStream, tag, handler, options, align, leaveOpen);
            } else if (dataStream is FileStream fs &&
                       IsAssembly(dataStream)) {
                LoadAssembly(dataStream, Path.GetDirectoryName(fs.Name) ?? "./", options, leaveOpen);
            } else {
                if (tag is not string path) {
                    throw new InvalidOperationException();
                }

                path = Path.GetFileName(path);
                var ext = Path.GetExtension(path)[1..].ToLower();
                switch (ext) {
                    case "ress":
                    case "resource":
                        Resources[path] = (tag, handler);
                        break;
                    default:
                        return;
                }
            }
        }

        public void LoadAssembly(Stream dataStream, string assemblyLocation, EquilibriumOptions? options, bool leaveOpen = false) {
            try {
                Assemblies.RemoveSearchDirectory(assemblyLocation);
                Assemblies.AddSearchDirectory(assemblyLocation);
                Assemblies.RegisterAssembly(AssemblyDefinition.ReadAssembly(new OffsetStream(dataStream, null, null, leaveOpen), new ReaderParameters(ReadingMode.Immediate) { InMemory = true, AssemblyResolver = Assemblies }));
                if (!leaveOpen) {
                    dataStream.Dispose();
                }
            } catch {
                // LOG THIS
            }
        }

        public static bool IsAssembly(Stream stream) {
            var pos = stream.Position;
            try {
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
    }
}
