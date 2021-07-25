using JetBrains.Annotations;

namespace Equilibrium.Meta {
    [PublicAPI]
    public interface IRenewable {
        public object Tag { get; set; }
        public IFileHandler Handler { get; set; }
    }
}
