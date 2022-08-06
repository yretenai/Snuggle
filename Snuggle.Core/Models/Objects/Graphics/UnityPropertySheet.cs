using System;
using System.Collections.Generic;
using Snuggle.Core.IO;
using Snuggle.Core.Models.Objects.Math;

namespace Snuggle.Core.Models.Objects.Graphics;

public record UnityPropertySheet(KeyValuePair<string, UnityTexEnv>[] Textures, KeyValuePair<string, float>[] Floats, KeyValuePair<string, ColorRGBA>[] Colors) {
    public static UnityPropertySheet Default { get; } = new(Array.Empty<KeyValuePair<string, UnityTexEnv>>(), Array.Empty<KeyValuePair<string, float>>(), Array.Empty<KeyValuePair<string, ColorRGBA>>());

    public static UnityPropertySheet FromReader(BiEndianBinaryReader reader, SerializedFile file) {
        var textureCount = reader.ReadInt32();
        var textures = new KeyValuePair<string, UnityTexEnv>[textureCount];
        for (var i = 0; i < textureCount; ++i) {
            textures[i] = new KeyValuePair<string, UnityTexEnv>(reader.ReadString32(), UnityTexEnv.FromReader(reader, file));
        }

        var floatCount = reader.ReadInt32();
        var floats = new KeyValuePair<string, float>[floatCount];
        for (var i = 0; i < floatCount; ++i) {
            floats[i] = new KeyValuePair<string, float>(reader.ReadString32(), reader.ReadSingle());
        }

        var colorCount = reader.ReadInt32();
        var colors = new KeyValuePair<string, ColorRGBA>[colorCount];
        for (var i = 0; i < colorCount; ++i) {
            colors[i] = new KeyValuePair<string, ColorRGBA>(reader.ReadString32(), reader.ReadStruct<ColorRGBA>());
        }

        return new UnityPropertySheet(textures, floats, colors);
    }

    public static void Seek(BiEndianBinaryReader reader, SerializedFile serializedFile) {
        var textureCount = reader.ReadInt32();
        for (var i = 0; i < textureCount; ++i) {
            reader.BaseStream.Position += reader.ReadInt32() + 4;
            reader.Align();
            reader.BaseStream.Position += 28;
        }

        var floatCount = reader.ReadInt32();
        for (var i = 0; i < floatCount; ++i) {
            reader.BaseStream.Position += reader.ReadInt32() + 4;
            reader.Align();
            reader.BaseStream.Position += 4;
        }

        var colorCount = reader.ReadInt32();
        for (var i = 0; i < colorCount; ++i) {
            reader.BaseStream.Position += reader.ReadInt32() + 4;
            reader.Align();
            reader.BaseStream.Position += 16;
        }
    }
}

public record BuildTextureStackReference(string GroupName, string ItemName) {
    public static BuildTextureStackReference Default { get; set; } = new(string.Empty, string.Empty);

    public static BuildTextureStackReference FromReader(BiEndianBinaryReader reader, SerializedFile file) {
        var group = reader.ReadString32();
        var item = reader.ReadString32();
        return new BuildTextureStackReference(group, item);
    }
}
