using System;
using System.IO;
using System.Linq;
using Equilibrium.Models;
using Equilibrium.Models.Bundle;
using JetBrains.Annotations;

namespace Equilibrium {
    [PublicAPI]
    public class Bundle : IDisposable {
        public Bundle(string path, bool cacheData = false) :
            this(File.OpenRead(path), path, FileSystemHandler.Instance.Value, false, cacheData) { }

        public Bundle(Stream dataStream, object tag, IFileHandler fileHandler, bool leaveOpen = false, bool cacheData = false) {
            using var reader = new BiEndianBinaryReader(dataStream, leaveOpen: leaveOpen);

            Header = UnityBundle.FromReader(reader);
            Container = Header.Format switch {
                UnityFormat.FS => UnityFS.FromReader(reader, Header),
                UnityFormat.Archive => throw new NotImplementedException(),
                UnityFormat.Web => throw new NotImplementedException(),
                UnityFormat.Raw => throw new NotImplementedException(),
                _ => throw new NotImplementedException(),
            };

            DataStart = dataStream.Position;
            Handler = fileHandler;
            Tag = tag;

            if (ShouldCacheData) {
                DataStream = new MemoryStream(Container.OpenFile(new UnityBundleBlock(0, (Container.BlockInfos ?? ArraySegment<UnityBundleBlockInfo>.Empty).Select(x => x.Size).Sum(), 0, ""), reader).ToArray());
            }
        }

        public UnityBundle Header { get; init; }
        public IUnityContainer? Container { get; init; }
        public long DataStart { get; set; }
        public object Tag { get; set; }
        public IFileHandler Handler { get; set; }
        public bool ShouldCacheData { get; private set; }
        private Stream? DataStream { get; }

        public void Dispose() {
            DataStream?.Dispose();
            GC.SuppressFinalize(this);
        }

        public Span<byte> OpenFile(string path) {
            if (Container == null) {
                return Span<byte>.Empty;
            }

            var block = Container.Blocks?.FirstOrDefault(x => x.Path.Equals(path, StringComparison.InvariantCultureIgnoreCase));
            if (block == null) {
                return Span<byte>.Empty;
            }

            BiEndianBinaryReader? reader = null;
            if (!ShouldCacheData ||
                DataStream == null) {
                reader = new BiEndianBinaryReader(Handler.OpenFile(Tag));
                reader.BaseStream.Seek(DataStart, SeekOrigin.Begin);
            }

            return Container.OpenFile(block, reader, DataStream);
        }
    }
}
