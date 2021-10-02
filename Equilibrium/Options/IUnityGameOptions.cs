using JetBrains.Annotations;

namespace Equilibrium.Options {
    [PublicAPI]
    public interface IUnityGameOptions {
        public int Version { get; set; }
        public IUnityGameOptions Migrate();
    }
}
