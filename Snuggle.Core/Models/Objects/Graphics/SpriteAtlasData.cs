using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Snuggle.Core.Implementations;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models.Objects.Math;

namespace Snuggle.Core.Models.Objects.Graphics;

[PublicAPI]
public record SpriteAtlasData(
    PPtr<Texture2D> Texture,
    PPtr<Texture2D> AlphaTexture,
    Rect TextureRect,
    Vector2 TextureRectOffset,
    Vector2 AtlasRectOffset,
    Vector4 UVTransform,
    float DownscaleMultiplier,
    uint Settings,
    List<SecondarySpriteTexture> SecondaryTextures) {
    public static SpriteAtlasData FromReader(BiEndianBinaryReader reader, SerializedFile file) {
        var texture = PPtr<Texture2D>.FromReader(reader, file);
        var alpha = PPtr<Texture2D>.FromReader(reader, file);
        var rect = reader.ReadStruct<Rect>();
        var offset = reader.ReadStruct<Vector2>();
        var atlasOffset = (file.Version >= UnityVersionRegister.Unity2017_2) ? reader.ReadStruct<Vector2>() : Vector2.Zero;
        var uv = reader.ReadStruct<Vector4>();
        var multiplier = reader.ReadSingle();
        var settings = reader.ReadUInt32();
        var secondaryTextures = new List<SecondarySpriteTexture>();
        if (file.Version >= UnityVersionRegister.Unity2020_2) {
            var count = reader.ReadInt32();
            secondaryTextures.EnsureCapacity(count);
            for (var i = 0; i < count; ++i) {
                secondaryTextures.Add(SecondarySpriteTexture.FromReader(reader, file));
            }
        }

        return new SpriteAtlasData(
            texture,
            alpha,
            rect,
            offset,
            atlasOffset,
            uv,
            multiplier,
            settings,
            secondaryTextures);
    }

    public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
        Texture.ToWriter(writer, serializedFile, targetVersion);
        AlphaTexture.ToWriter(writer, serializedFile, targetVersion);
        writer.WriteStruct(TextureRect);
        writer.WriteStruct(TextureRectOffset);
        if (targetVersion >= UnityVersionRegister.Unity2017_2) {
            writer.WriteStruct(AtlasRectOffset);
        }

        writer.WriteStruct(UVTransform);
        writer.Write(Settings);

        if (targetVersion >= UnityVersionRegister.Unity2020_2) {
            writer.Write(SecondaryTextures.Count);
            foreach (var secondaryTexture in SecondaryTextures) {
                secondaryTexture.ToWriter(writer, serializedFile, targetVersion);
            }
        }
    }
}
