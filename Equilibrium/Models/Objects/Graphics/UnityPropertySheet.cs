using System.Collections.Generic;
using Equilibrium.IO;
using Equilibrium.Models.Objects.Math;
using JetBrains.Annotations;

namespace Equilibrium.Models.Objects.Graphics {
    [PublicAPI]
    public record UnityPropertySheet(
        Dictionary<string, UnityTexEnv> Textures,
        Dictionary<string, float> Floats,
        Dictionary<string, ColorRGBA> Colors) {
        public static UnityPropertySheet Default { get; } = new(new Dictionary<string, UnityTexEnv>(), new Dictionary<string, float>(), new Dictionary<string, ColorRGBA>());

        public static UnityPropertySheet FromReader(BiEndianBinaryReader reader, SerializedFile file) {
            var textureCount = reader.ReadInt32();
            var textures = new Dictionary<string, UnityTexEnv>();
            textures.EnsureCapacity(textureCount);
            for (var i = 0; i < textureCount; ++i) {
                textures[reader.ReadString32()] = UnityTexEnv.FromReader(reader, file);
            }

            var floatCount = reader.ReadInt32();
            var floats = new Dictionary<string, float>();
            floats.EnsureCapacity(floatCount);
            for (var i = 0; i < floatCount; ++i) {
                floats[reader.ReadString32()] = reader.ReadSingle();
            }

            var colorCount = reader.ReadInt32();
            var colors = new Dictionary<string, ColorRGBA>();
            colors.EnsureCapacity(colorCount);
            for (var i = 0; i < colorCount; ++i) {
                colors[reader.ReadString32()] = reader.ReadStruct<ColorRGBA>();
            }

            return new UnityPropertySheet(textures, floats, colors);
        }
    }

    [PublicAPI]
    public record BuildTextureStackReference(
        string GroupName,
        string ItemName) {
        public static BuildTextureStackReference Default { get; set; } = new(string.Empty, string.Empty);

        public static BuildTextureStackReference FromReader(BiEndianBinaryReader reader, SerializedFile file) {
            var group = reader.ReadString32();
            var item = reader.ReadString32();
            return new BuildTextureStackReference(group, item);
        }
    }
}
