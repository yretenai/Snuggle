using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Meta.Interfaces;
using Equilibrium.Meta.Options;
using Equilibrium.Models.Bundle;
using JetBrains.Annotations;

namespace Equilibrium {
    [PublicAPI]
    public class Bundle : IDisposable, IRenewable {
        public Bundle(string path, EquilibriumOptions options) :
            this(File.OpenRead(path), path, FileStreamHandler.Instance.Value, options) { }

        public Bundle(Stream dataStream, object tag, IFileHandler fileHandler, EquilibriumOptions options, bool leaveOpen = false) {
            using var reader = new BiEndianBinaryReader(dataStream, true, leaveOpen);

            Options = options;

            Header = UnityBundle.FromReader(reader, options);
            Container = Header.Format switch {
                UnityFormat.FS => UnityFS.FromReader(reader, Header, options),
                UnityFormat.Archive => throw new NotImplementedException(),
                UnityFormat.Web => UnityRaw.FromReader(reader, Header, options),
                UnityFormat.Raw => UnityRaw.FromReader(reader, Header, options),
                _ => throw new InvalidOperationException(),
            };

            DataStart = dataStream.Position;
            Handler = fileHandler;
            Tag = fileHandler.GetTag(tag, this);

            if (Options.CacheData) {
                CacheData(reader);
            }
        }

        public UnityBundle Header { get; init; }
        public IUnityContainer Container { get; init; }
        public long DataStart { get; set; }
        public EquilibriumOptions Options { get; init; }
        public Stream? DataStream { get; private set; }

        public void Dispose() {
            DataStream?.Dispose();
            Handler.Dispose();
            GC.SuppressFinalize(this);
        }

        public object Tag { get; set; }
        public IFileHandler Handler { get; set; }

        public static Bundle[] OpenBundleSequence(Stream dataStream, object tag, IFileHandler handler, int align = 1, EquilibriumOptions? options = null, bool leaveOpen = false) {
            var bundles = new List<Bundle>();
            while (dataStream.Position < dataStream.Length) {
                var start = dataStream.Position;
                if (!IsBundleFile(dataStream)) {
                    break;
                }

                var bundle = new Bundle(new OffsetStream(dataStream), new MultiMetaInfo(tag, start, 0), handler, options ?? EquilibriumOptions.Default, true);
                bundles.Add(bundle);
                dataStream.Seek(start + bundle.Container.Length, SeekOrigin.Begin);

                if (align > 1) {
                    if (dataStream.Position % align == 0) {
                        continue;
                    }

                    var delta = (int) (align - dataStream.Position % align);
                    dataStream.Seek(delta, SeekOrigin.Current);
                }
            }

            if (!leaveOpen) {
                dataStream.Dispose();
            }

            return bundles.ToArray();
        }

        public void CacheData(BiEndianBinaryReader? reader = null) {
            if (DataStream != null) {
                return;
            }

            var shouldDispose = false;
            if (reader == null) {
                reader = new BiEndianBinaryReader(Handler.OpenFile(Tag));
                shouldDispose = true;
            }

            reader.BaseStream.Seek(DataStart, SeekOrigin.Begin);

            DataStream = Container.OpenFile(new UnityBundleBlock(0, Container.BlockInfos.Select(x => x.Size).Sum(), 0, ""), Options, reader);

            if (shouldDispose) {
                reader.Dispose();
            }
        }

        public void ClearCache() {
            DataStream?.Dispose();
            DataStream = null;
        }

        public Stream OpenFile(string path) {
            var block = Container.Blocks.FirstOrDefault(x => x.Path.Equals(path, StringComparison.InvariantCultureIgnoreCase));
            return block == null ? Stream.Null : OpenFile(block);
        }

        public Stream OpenFile(UnityBundleBlock block) {
            BiEndianBinaryReader? reader = null;
            if (DataStream == null) {
                reader = new BiEndianBinaryReader(Handler.OpenFile(Tag), true);
                reader.BaseStream.Seek(DataStart, SeekOrigin.Begin);
            }

            var data = Container.OpenFile(block, Options, reader, DataStream);
            reader?.Dispose();
            data.Seek(0, SeekOrigin.Begin);
            return data;
        }

        public Stream ToStream(UnityBundleBlock[] blocks, Stream dataStream, BundleSerializationOptions? serializationOptions) {
            var stream = new MemoryStream();
            using var writer = new BiEndianBinaryWriter(stream, true, true);
            Header.ToWriter(writer, Options);
            Container.ToWriter(writer, Header, Options, blocks, dataStream, serializationOptions ?? BundleSerializationOptions.Default);
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        public static bool IsBundleFile(Stream stream) {
            using var reader = new BiEndianBinaryReader(stream, true, true);
            return IsBundleFile(reader);
        }

        private static bool IsBundleFile(BiEndianBinaryReader reader) {
            var pos = reader.BaseStream.Position;
            try {
                if (reader.PeekChar() != 'U') {
                    return false;
                }

                return reader.ReadNullString(0x10) is "UnityFS" or "UnityWeb" or "UnityRaw" or "UnityArchive";
            } catch {
                return false;
            } finally {
                reader.BaseStream.Seek(pos, SeekOrigin.Begin);
            }
        }
    }
}
