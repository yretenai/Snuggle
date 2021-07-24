using System;
using System.Collections.Generic;
using System.IO;
using Equilibrium.IO;
using Equilibrium.Models.Bundle;
using Equilibrium.Models.IO;
using JetBrains.Annotations;

namespace Equilibrium {
    [PublicAPI]
    public class AssetCollection : IDisposable {
        public List<Bundle> Bundles { get; } = new();
        public Dictionary<string, SerializedFile> Files { get; } = new(StringComparer.InvariantCultureIgnoreCase);
        public Dictionary<string, (object Tag, IFileHandler Handler)> ResourceStreams { get; } = new(StringComparer.InvariantCultureIgnoreCase);
        public Dictionary<string, (object Tag, IFileHandler Handler)> Resources { get; } = new(StringComparer.InvariantCultureIgnoreCase);

        public void LoadBundle(Bundle bundle) {
            var handler = new BundleStreamHandler(bundle);
            foreach (var block in bundle.Container.Blocks) {
                if (block.Flags.HasFlag(UnityBundleBlockFlags.SerializedFile)) {
                    LoadSerializedFile(new MemoryStream(bundle.OpenFile(block)) { Position = 0 }, block, handler);
                } else {
                    var ext = Path.GetExtension(block.Path).ToLower();
                    var stripped = block.Path[..block.Path.LastIndexOf('.')] ?? block.Path;
                    switch (ext) {
                        case "ress":
                            ResourceStreams[stripped] = (block, handler);
                            break;
                        case "resource":
                            Resources[stripped] = (block, handler);
                            break;
                        default:
                            throw new NotImplementedException(ext);
                    }
                }
            }

            Bundles.Add(bundle);
        }

        public void LoadBundle(Stream dataStream, object tag, IFileHandler handler, bool leaveOpen = false) => LoadBundle(new Bundle(dataStream, tag, handler, leaveOpen));

        public void LoadBundle(string path, bool leaveOpen = false) => LoadBundle(File.OpenRead(path), path, FileStreamHandler.Instance.Value, leaveOpen);

        public void LoadBundleSequence(Stream dataStream, object tag, IFileHandler handler, int align = 1, bool cacheData = false) {
            var bundles = Bundle.OpenBundleSequence(dataStream, tag, handler, align, false, cacheData);
            foreach (var bundle in bundles) {
                LoadBundle(bundle);
            }
        }

        public void LoadBundleSequence(string path, int align = 1, bool cacheData = false) => LoadBundleSequence(File.OpenRead(path), path, MultiStreamHandler.FileInstance.Value, align, cacheData);

        public void LoadSerializedFile(Stream dataStream, object tag, IFileHandler handler, bool leaveOpen = false) {
            var path = tag switch {
                UnityBundleBlock block => block.Path,
                string str => Path.GetFileName(str),
                _ => throw new NotImplementedException(),
            };

            if (Files.ContainsKey(path)) {
                if (!leaveOpen) {
                    dataStream.Close();
                }

                return;
            }

            var file = new SerializedFile(dataStream, tag, handler, leaveOpen) { Assets = this };
            // TODO process objects
            Files[path] = file;
        }

        public void LoadSerializedFile(string path) => LoadSerializedFile(File.OpenRead(path), path, FileStreamHandler.Instance.Value);

        public void LoadFile(string path) => LoadFile(File.OpenRead(path), path, MultiStreamHandler.FileInstance.Value);

        private void LoadFile(Stream dataStream, object tag, IFileHandler handler, int align = 1, bool leaveOpen = false) {
            if (SerializedFile.IsSerializedFile(dataStream)) {
                LoadSerializedFile(dataStream, tag, handler, leaveOpen);
            } else if (Bundle.IsBundleFile(dataStream)) {
                LoadBundleSequence(dataStream, tag, handler, align, leaveOpen);
            } else {
                if (tag is not string path) {
                    throw new NotImplementedException();
                }

                path = Path.GetFileName(path);
                var ext = Path.GetExtension(path).ToLower();
                var stripped = Path.GetFileNameWithoutExtension(path);
                switch (ext) {
                    case "ress":
                        ResourceStreams[stripped] = (tag, handler);
                        break;
                    case "resource":
                        Resources[stripped] = (tag, handler);
                        break;
                    default:
                        throw new NotImplementedException(ext);
                }
            }
        }

        public void Dispose() {
            foreach (var bundle in Bundles) {
                bundle.Dispose();
            }

            Bundles.Clear();
            GC.SuppressFinalize(this);
        }
    }
}
