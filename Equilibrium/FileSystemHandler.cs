using System;
using System.IO;
using Equilibrium.Models;
using JetBrains.Annotations;

namespace Equilibrium {
    [PublicAPI]
    public class FileSystemHandler : IFileHandler {
        public static Lazy<FileSystemHandler> Instance { get; } = new();

        public Stream OpenFile(object tag) {
            string path = tag switch {
                FileInfo fi => fi.FullName,
                string str => str,
                _ => throw new NotImplementedException(),
            };

            return File.OpenRead(path);
        }
    }
}
