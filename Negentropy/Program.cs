using Equilibrium;
using Equilibrium.Options;

namespace Negentropy {
    internal static class Program {
        private static void Main(string[] args) {
            var assets = new AssetCollection();
            foreach (var arg in args) {
                assets.LoadFile(arg, EquilibriumOptions.Default);
            }
        }
    }
}
