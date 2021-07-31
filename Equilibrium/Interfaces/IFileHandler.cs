using System;
using System.IO;
using JetBrains.Annotations;

namespace Equilibrium.Interfaces {
    [PublicAPI]
    public interface IFileHandler : IDisposable {
        public Stream OpenFile(object tag);
        public object GetTag(object baseTag, object parent);
    }
}
