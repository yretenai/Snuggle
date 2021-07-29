using Equilibrium;
using Equilibrium.Meta.Options;

namespace Negentropy {
    internal static class Program {
        private static void Main(string[] args) {
            var assets = new AssetCollection();
            assets.LoadFile(args[0], EquilibriumOptions.Default with { CacheData = true });
        }
    }
}
