using System.IO;
using System.Text;
using Equilibrium.Implementations;

namespace Entropy.Handlers {
    public static class EntropyMaterialFile {
        public static void SaveMaterial(Material? material, string path) {
            if (material == null) {
                return;
            }

            var matPath = Path.Combine(path, $"{material.Name}_{material.PathId}.mat");
            if (File.Exists(matPath)) {
                return;
            }
            
            using var materialStream = new FileStream(matPath, FileMode.Create);
            using var materialWriter = new StreamWriter(materialStream, Encoding.UTF8);

            foreach (var (name, (texEnv, scaleEnv, offsetEnv)) in material.SavedProperties.Textures) {
                materialWriter.WriteLine($"{name} = Texture {{ Name = {texEnv.Value?.Name ?? "null"}, PathId = {texEnv.PathId}, Scale = {scaleEnv}, Offset = {offsetEnv} }}");
            }

            foreach (var (name, floatEnv) in material.SavedProperties.Floats) {
                materialWriter.WriteLine($"{name} = Float {{ Value = {floatEnv} }}");
            }

            foreach (var (name, colorEnv) in material.SavedProperties.Colors) {
                materialWriter.WriteLine($"{name} = {colorEnv}");
            }
        }
    }
}
