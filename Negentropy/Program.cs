using System.IO;
using System.Linq;
using Equilibrium;
using Equilibrium.Implementations;
using Equilibrium.Models;
using Equilibrium.Options;

namespace Negentropy {
    internal static class Program {
        private static void Main(string[] args) {
            var assets = new AssetCollection();
            foreach (var arg in args) {
                assets.LoadFile(arg, EquilibriumOptions.Default with { CacheData = true });
            }

            foreach (var texture in assets.Files.First().Value.Objects.Values.Where(x => x.ClassId.Equals(UnityClassId.Texture2D)).Cast<Texture2D>()) {
                using var output = File.OpenWrite($"{texture}.dds");
                texture.Deserialize(ObjectDeserializationOptions.Default);
                using var dds = texture.ToDDS();
                dds.CopyTo(output);
                texture.Free();
            }
        }
    }
}
