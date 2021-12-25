using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Snuggle.Core.Implementations;
using Snuggle.Core.Models.Objects.Graphics;
using Snuggle.Core.Models.Objects.Math;
using Snuggle.Core.Options;

namespace Snuggle.Converters;

public static class SnuggleMaterialFile {
    public static void Save(Material? material, string path, bool isDir = true) {
        if (material == null) {
            return;
        }

        var matPath = path;
        if (isDir) {
            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }

            matPath = Path.Combine(path, $"{material.Name}_{material.PathId}.json");
        }

        if (File.Exists(matPath)) {
            return;
        }

        var textures = material.SavedProperties.Textures.ToDictionary(x => x.Key, x => new TextureInfo(x.Value));
        var floats = material.SavedProperties.Floats;
        var colors = material.SavedProperties.Colors;

        using var materialStream = new FileStream(matPath, FileMode.Create);
        using var materialWriter = new StreamWriter(materialStream, Encoding.UTF8);
        JsonSerializer.Serialize(materialStream, new { Textures = textures, Floats = floats, Colors = colors }, SnuggleCoreOptions.JsonOptions);
    }

    public record struct TextureInfo(long PathId, string Name, Vector2 Scale, Vector2 Offset) {
        public TextureInfo(UnityTexEnv env) : this(env.Texture.PathId, env.Texture.Value?.Name ?? "null", env.Scale, env.Offset) { }
    }
}
