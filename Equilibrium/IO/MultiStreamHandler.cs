using System;
using System.IO;
using Equilibrium.Models.IO;
using JetBrains.Annotations;

namespace Equilibrium.IO {
    [PublicAPI]
    public class MultiStreamHandler : IFileHandler {
        public static Lazy<MultiStreamHandler> Instance { get; } = new();

        public void Dispose() {
            GC.SuppressFinalize(this);
        }

        public Stream OpenFile(object tag) {
            if (tag is string path) {
                tag = new FileInfo(path);
            }

            var (filePath, offset, size) = tag switch {
                MultiMetaInfo meta => meta,
                FileInfo fi => new MultiMetaInfo(fi.FullName, 0, fi.Length),
                _ => throw new FileNotFoundException(),
            };

            return new OffsetStream(File.OpenRead(filePath), offset, size);
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
