using System.IO;

namespace Equilibrium.Models {
    public interface IFileHandler {
        public Stream OpenFile(object tag);
    }
}
