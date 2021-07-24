using System;
using System.IO;

namespace Equilibrium.Models.IO {
    public interface IFileHandler : IDisposable {
        public Stream OpenFile(object tag);
    }
}
