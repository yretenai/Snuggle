using System;
using System.IO;
using Equilibrium.Interfaces;
using JetBrains.Annotations;

namespace Equilibrium.IO {
    [PublicAPI]
    public class FileStreamHandler : IFileHandler {
        public static Lazy<FileStreamHandler> Instance { get; } = new();

        public Stream OpenFile(object tag) {
            string path = tag switch {
                FileInfo fi => fi.FullName,
                string str => str,
                _ => throw new NotSupportedException($"{tag.GetType().FullName} is not supported"),
            };

            return File.OpenRead(path);
        }

        public object GetTag(object baseTag, object parent) => baseTag;

        public void Dispose() {
            GC.SuppressFinalize(this);
        }
    }
}
