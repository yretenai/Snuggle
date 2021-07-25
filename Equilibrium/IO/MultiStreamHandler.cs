using System;
using System.IO;
using Equilibrium.Meta;
using JetBrains.Annotations;

namespace Equilibrium.IO {
    [PublicAPI]
    public class MultiStreamHandler : IFileHandler {
        public MultiStreamHandler(IFileHandler handler) => UnderlyingHandler = handler;

        public static Lazy<MultiStreamHandler> FileInstance { get; } = new(() => new MultiStreamHandler(new FileStreamHandler()));

        public IFileHandler UnderlyingHandler { get; }

        public void Dispose() {
            GC.SuppressFinalize(this);
        }

        public Stream OpenFile(object tag) {
            var (subTag, offset, size) = tag switch {
                MultiMetaInfo meta => meta,
                string str => new MultiMetaInfo(str, 0, new FileInfo(str).Length),
                _ => throw new FileNotFoundException(),
            };

            var stream = UnderlyingHandler.OpenFile(subTag);

            return new OffsetStream(stream, offset, size);
        }

        public object GetTag(object baseTag, object parent) {
            if (baseTag is not MultiMetaInfo meta) {
                return baseTag;
            }

            var length = 0L;
            if (parent is Bundle bundle) {
                length = bundle.Container.Length;
            }

            if (length == 0) {
                return baseTag;
            }

            return meta with { Size = length };
        }
    }
}
