using JetBrains.Annotations;

namespace Equilibrium.Interfaces {
    [PublicAPI]
    public interface IRenewable {
        public object Tag { get; set; }
        public IFileHandler Handler { get; set; }
    }
}
