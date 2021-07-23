using System.IO;

namespace Equilibrium.Models.IO {
    public interface IFileHandler {
        public Stream OpenFile(object tag);
    }
}
