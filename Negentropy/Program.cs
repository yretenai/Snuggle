using System;
using System.Linq;
using System.Text.Json;
using Equilibrium;
using Equilibrium.Implementations;
using Equilibrium.Meta.Options;
using Equilibrium.Models;

namespace Negentropy {
    internal static class Program {
        private static void Main(string[] args) {
            var assets = new AssetCollection();
            foreach (var arg in args) {
                assets.LoadFile(arg, EquilibriumOptions.Default with { CacheData = true });
            }

            foreach (var monoBehaviour in assets.Files.First().Value.Objects.Values.Where(x => x.ClassId.Equals(UnityClassId.MonoBehaviour)).Cast<MonoBehaviour>()) {
                monoBehaviour.Deserialize(ObjectDeserializationOptions.Default);
                Console.WriteLine(JsonSerializer.Serialize(monoBehaviour.Data, new JsonSerializerOptions { WriteIndented = true }));
            }
        }
    }
}
