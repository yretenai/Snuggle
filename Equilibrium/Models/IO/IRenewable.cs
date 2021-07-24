using JetBrains.Annotations;

namespace Equilibrium.Models.IO {
    [PublicAPI]
    public interface IRenewable {
        public object Tag { get; set; }
        public IFileHandler Handler { get; set; }
    }
}
