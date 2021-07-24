using System;
using System.IO;
using Equilibrium.Models.IO;
using JetBrains.Annotations;

namespace Equilibrium.IO {
    [PublicAPI]
    public class BundleStreamHandler : IFileHandler {
        public BundleStreamHandler(Bundle bundleFile) => BundleFile = bundleFile;

        public Bundle BundleFile { get; }

        public Stream OpenFile(object tag) {
            string path = tag switch {
                string str => str,
                _ => throw new NotImplementedException(),
            };

            return new MemoryStream(BundleFile.OpenFile(path)) { Position = 0 };
        }

        public object GetTag(object baseTag, object parent) {
            return baseTag;
        }

        public void Dispose() {
            BundleFile.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
