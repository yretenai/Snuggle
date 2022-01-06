using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using DragonLib;
using JetBrains.Annotations;
using Snuggle.Core.Implementations;
using Snuggle.Core.Models.Objects.Graphics;
using Snuggle.Core.Models.Objects.Math;
using Snuggle.Core.Options;

namespace Snuggle.Converters;

public static class SnuggleMaterialFile {
    public static void Save(Material? material, string path, bool isDir) {
        if (material == null) {
            return;
        }

        string matPath;
        if (isDir) {
            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }

            matPath = Path.Combine(path, $"{material.Name}_{material.PathId}.json");
        } else {
            matPath = Path.ChangeExtension(path, ".json");
        }

        if (File.Exists(matPath)) {
            return;
        }

        matPath.EnsureDirectoryExists();

        var textures = material.SavedProperties.Textures.Where(x => !x.Value.Texture.IsNull).ToDictionary(x => x.Key, x => new TextureInfo(x.Value));
        var floats = material.SavedProperties.Floats.Select(x => float.IsNormal(x.Value) ? x : new KeyValuePair<string, float>(x.Key, 0));
        var colors = material.SavedProperties.Colors.Select(x => new KeyValuePair<string, ColorRGBA>(x.Key, x.Value.GetJSONSafe()));

        using var materialStream = new FileStream(matPath, FileMode.Create);
        using var materialWriter = new StreamWriter(materialStream, Encoding.UTF8);
        JsonSerializer.Serialize(materialStream, new { Textures = textures, Floats = floats, Colors = colors }, SnuggleCoreOptions.JsonOptions);
    }

    [PublicAPI]
    public record struct TextureInfo(long PathId, string? Name, Vector2 Scale, Vector2 Offset) {
        public TextureInfo(UnityTexEnv env) : this(env.Texture.PathId, string.IsNullOrWhiteSpace(env.Texture.Value?.ObjectContainerPath) ? env.Texture.Value?.Name : env.Texture.Value?.ObjectContainerPath, env.Scale.GetJSONSafe(), env.Offset.GetJSONSafe()) { }
    }
}
