using System;
using System.Linq;
using Equilibrium;
using Equilibrium.Meta.Options;
using Equilibrium.Models;

namespace Negentropy {
    internal static class Program {
        private static void Main(string[] args) {
            var assets = new AssetCollection();
            assets.LoadFile(args[0], EquilibriumOptions.Default with { CacheData = true });

            foreach (var type in assets.Files.SelectMany(asset => asset.Value.Types).Where(x => x.ClassId.Equals(UnityClassId.MonoBehaviour))) {
                Console.WriteLine(type.TypeTree.PrintLayout(false, true));
            }
        }
    }
}
