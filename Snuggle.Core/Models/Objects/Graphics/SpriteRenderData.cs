using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using DragonLib.IO;
using JetBrains.Annotations;
using Snuggle.Core.Exceptions;
using Snuggle.Core.Implementations;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models.Objects.Math;
using Snuggle.Core.Options;

namespace Snuggle.Core.Models.Objects.Graphics;

[PublicAPI]
public record SpriteRenderData(
    PPtr<Texture2D> Texture,
    PPtr<Texture2D> AlphaTexture,
    List<SecondarySpriteTexture> SecondaryTextures,
    List<Submesh> Submeshes,
    VertexData VertexData,
    List<Matrix4X4> BindPose,
    Rect TextureRect,
    Vector2 TextureRectOffset,
    Vector2 AtlasRectOffset,
    SpriteSettings Settings,
    Vector4 UVTransform,
    float DownscaleMultiplier) {
    public static SpriteRenderData Default { get; } = new(
        PPtr<Texture2D>.Null,
        PPtr<Texture2D>.Null,
        new List<SecondarySpriteTexture>(),
        new List<Submesh>(),
        VertexData.Default,
        new List<Matrix4X4>(),
        Rect.Zero,
        Vector2.Zero,
        Vector2.Zero,
        SpriteSettings.Default,
        Vector4.Zero,
        1);

    private long VerticesStart { get; init; } = -1;
    private long IndicesStart { get; init; } = -1;

    private long SkinStart { get; init; } = -1;

    [JsonIgnore]
    public Memory<Vector3>? Vertices { get; set; }

    [JsonIgnore]
    public Memory<byte>? Indices { get; set; }

    [JsonIgnore]
    public List<BoneWeight>? Skin { get; set; }

    private bool ShouldDeserializeVertices => VerticesStart > -1 && Vertices == null;
    private bool ShouldDeserializeIndices => IndicesStart > -1 && Indices == null;
    private bool ShouldDeserializeSkin => SkinStart > -1 && Skin == null;

    [JsonIgnore]
    public bool ShouldDeserialize => ShouldDeserializeVertices || ShouldDeserializeIndices || ShouldDeserializeSkin || VertexData.ShouldDeserialize;

    public static SpriteRenderData FromReader(BiEndianBinaryReader reader, SerializedFile serializedFile) {
        var texture = PPtr<Texture2D>.FromReader(reader, serializedFile);
        var alpha = serializedFile.Version >= UnityVersionRegister.Unity5_2 ? PPtr<Texture2D>.FromReader(reader, serializedFile) : PPtr<Texture2D>.Null;
        var secondary = new List<SecondarySpriteTexture>();

        int count;
        if (serializedFile.Version >= UnityVersionRegister.Unity2019_1) {
            count = reader.ReadInt32();
            secondary.EnsureCapacity(count);
            for (var i = 0; i < count; ++i) {
                secondary.Add(SecondarySpriteTexture.FromReader(reader, serializedFile));
            }
        }

        var submeshes = new List<Submesh>();
        var vertexOffset = -1L;
        if (serializedFile.Version >= UnityVersionRegister.Unity5_6) {
            count = reader.ReadInt32();
            submeshes.EnsureCapacity(count);
            for (var i = 0; i < count; ++i) {
                submeshes.Add(Submesh.FromReader(reader, serializedFile));
            }
        } else {
            vertexOffset = reader.BaseStream.Position;
            count = reader.ReadInt32();
            reader.BaseStream.Seek(count * 12, SeekOrigin.Current);
        }

        var indexOffset = reader.BaseStream.Position;
        count = reader.ReadInt32();
        reader.BaseStream.Seek(count, SeekOrigin.Current);
        reader.Align();

        var vertexData = VertexData.Default;
        if (serializedFile.Version >= UnityVersionRegister.Unity5_6) {
            vertexData = VertexData.FromReader(reader, serializedFile);
        }

        var bindPose = new List<Matrix4X4>();
        var skinOffset = -1L;
        if (serializedFile.Version >= UnityVersionRegister.Unity2018_1) {
            count = reader.ReadInt32();
            bindPose.EnsureCapacity(count);
            bindPose.AddRange(reader.ReadArray<Matrix4X4>(count).ToArray());

            if (serializedFile.Version <= UnityVersionRegister.Unity2018_2) {
                count = reader.ReadInt32();
                skinOffset = reader.BaseStream.Position;
                reader.BaseStream.Seek(count * 32, SeekOrigin.Current);
            }
        }

        var rect = reader.ReadStruct<Rect>();
        var offset = reader.ReadStruct<Vector2>();
        var atlasOffset = serializedFile.Version >= UnityVersionRegister.Unity5_6 ? reader.ReadStruct<Vector2>() : Vector2.Zero;
        var settings = reader.ReadUInt32();
        var uv = reader.ReadStruct<Vector4>();
        var multiplier = reader.ReadSingle();
        return new SpriteRenderData(
            texture,
            alpha,
            secondary,
            submeshes,
            vertexData,
            bindPose,
            rect,
            offset,
            atlasOffset,
            BitPacked.Unpack<SpriteSettings>(settings),
            uv,
            multiplier) { VerticesStart = vertexOffset, IndicesStart = indexOffset, SkinStart = skinOffset };
    }

    public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
        if (ShouldDeserialize) {
            throw new IncompleteDeserialization();
        }

        Texture.ToWriter(writer, serializedFile, targetVersion);
        if (targetVersion >= UnityVersionRegister.Unity5_2) {
            AlphaTexture.ToWriter(writer, serializedFile, targetVersion);
        }

        if (targetVersion >= UnityVersionRegister.Unity2019_1) {
            writer.Write(SecondaryTextures.Count);
            foreach (var secondaryTexture in SecondaryTextures) {
                secondaryTexture.ToWriter(writer, serializedFile, targetVersion);
            }
        }

        if (targetVersion >= UnityVersionRegister.Unity5_2) {
            writer.Write(Submeshes.Count);
            foreach (var submesh in Submeshes) {
                submesh.ToWriter(writer, serializedFile, targetVersion);
            }
        } else {
            writer.Write(Vertices?.Length ?? 0);
            if (Vertices.HasValue) {
                writer.WriteArray(Vertices.Value.Span);
            }
        }

        writer.Write(Indices!.Value.Length);
        writer.WriteArray(Indices.Value.Span);
        writer.Align();

        if (targetVersion >= UnityVersionRegister.Unity5_6) {
            VertexData.ToWriter(writer, serializedFile, targetVersion);
        }

        if (targetVersion >= UnityVersionRegister.Unity2018_1) {
            writer.Write(BindPose.Count);
            writer.WriteArray(BindPose);

            if (targetVersion <= UnityVersionRegister.Unity2018_2) {
                writer.Write(Skin!.Count);
                foreach (var skin in Skin) {
                    skin.ToWriter(writer, serializedFile, targetVersion);
                }
            }
        }

        writer.WriteStruct(TextureRect);
        writer.WriteStruct(TextureRectOffset);
        writer.WriteStruct(AtlasRectOffset);
        writer.Write((uint) BitPacked.Pack(Settings));
        writer.WriteStruct(UVTransform);
        writer.Write(DownscaleMultiplier);
    }

    public void Deserialize(BiEndianBinaryReader reader, SerializedFile serializedFile, ObjectDeserializationOptions options) {
        if (ShouldDeserializeIndices) {
            reader.BaseStream.Seek(IndicesStart, SeekOrigin.Begin);
            var indicesCount = reader.ReadInt32();
            Indices = reader.ReadMemory(indicesCount);
        }

        if (ShouldDeserializeVertices) {
            reader.BaseStream.Seek(VerticesStart, SeekOrigin.Begin);
            var vertexCount = reader.ReadInt32();
            Vertices = reader.ReadMemory<Vector3>(vertexCount);
        }

        if (ShouldDeserializeSkin) {
            reader.BaseStream.Seek(SkinStart, SeekOrigin.Begin);
            var boneWeightsCount = reader.ReadInt32();
            Skin = new List<BoneWeight>();
            Skin.EnsureCapacity(boneWeightsCount);
            for (var i = 0; i < boneWeightsCount; ++i) {
                Skin.Add(BoneWeight.FromReader(reader, serializedFile));
            }
        }

        if (VertexData.ShouldDeserialize) {
            VertexData.Deserialize(reader, serializedFile, options);
        }
    }

    public void Free() {
        Vertices = null;
        Indices = null;
        Skin = null;
        VertexData.Free();
    }
}
