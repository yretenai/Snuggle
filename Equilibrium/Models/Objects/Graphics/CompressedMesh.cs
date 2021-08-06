using System.Text.Json.Serialization;
using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Options;
using JetBrains.Annotations;

namespace Equilibrium.Models.Objects.Graphics {
    [PublicAPI]
    public record CompressedMesh(
        PackedBitVector Vertices,
        PackedBitVector UVs,
        PackedBitVector Normals,
        PackedBitVector Tangents,
        PackedBitVector Weights,
        PackedBitVector NormalSigns,
        PackedBitVector TangentSigns,
        PackedBitVector FloatColors,
        PackedBitVector BoneIndices,
        PackedBitVector Triangles,
        uint UVInfo) {
        public static CompressedMesh Default { get; } = new(
            PackedBitVector.Default,
            PackedBitVector.Default,
            PackedBitVector.Default,
            PackedBitVector.Default,
            PackedBitVector.Default,
            PackedBitVector.Default,
            PackedBitVector.Default,
            PackedBitVector.Default,
            PackedBitVector.Default,
            PackedBitVector.Default,
            0);

        [JsonIgnore]
        public bool ShouldDeserialize =>
            Vertices.ShouldDeserialize ||
            UVs.ShouldDeserialize ||
            Normals.ShouldDeserialize ||
            Tangents.ShouldDeserialize ||
            Weights.ShouldDeserialize ||
            NormalSigns.ShouldDeserialize ||
            TangentSigns.ShouldDeserialize ||
            FloatColors.ShouldDeserialize ||
            BoneIndices.ShouldDeserialize ||
            Triangles.ShouldDeserialize;

        public static CompressedMesh FromReader(BiEndianBinaryReader reader, SerializedFile file) {
            var vertices = PackedBitVector.FromReader(reader, file, true);
            var uvs = PackedBitVector.FromReader(reader, file, true);
            var normals = PackedBitVector.FromReader(reader, file, true);
            var tangents = PackedBitVector.FromReader(reader, file, true);
            var weights = PackedBitVector.FromReader(reader, file, false);
            var normalSigns = PackedBitVector.FromReader(reader, file, false);
            var tangentSigns = PackedBitVector.FromReader(reader, file, false);
            var floatColors = PackedBitVector.FromReader(reader, file, true);
            var boneIndices = PackedBitVector.FromReader(reader, file, false);
            var triangles = PackedBitVector.FromReader(reader, file, false);
            var uvInfo = reader.ReadUInt32();
            return new CompressedMesh(vertices, uvs, normals, tangents, weights, normalSigns, tangentSigns, floatColors, boneIndices, triangles, uvInfo);
        }

        public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
            Vertices.ToWriter(writer, serializedFile, targetVersion);
            UVs.ToWriter(writer, serializedFile, targetVersion);
            Normals.ToWriter(writer, serializedFile, targetVersion);
            Tangents.ToWriter(writer, serializedFile, targetVersion);
            Weights.ToWriter(writer, serializedFile, targetVersion);
            NormalSigns.ToWriter(writer, serializedFile, targetVersion);
            TangentSigns.ToWriter(writer, serializedFile, targetVersion);
            FloatColors.ToWriter(writer, serializedFile, targetVersion);
            BoneIndices.ToWriter(writer, serializedFile, targetVersion);
            Triangles.ToWriter(writer, serializedFile, targetVersion);
            writer.Write(UVInfo);
        }

        public void Deserialize(BiEndianBinaryReader reader, SerializedFile serializedFile, ObjectDeserializationOptions options) {
            if (Vertices.ShouldDeserialize) {
                Vertices.Deserialize(reader, serializedFile, options);
            }

            if (UVs.ShouldDeserialize) {
                UVs.Deserialize(reader, serializedFile, options);
            }

            if (Normals.ShouldDeserialize) {
                Normals.Deserialize(reader, serializedFile, options);
            }

            if (Tangents.ShouldDeserialize) {
                Tangents.Deserialize(reader, serializedFile, options);
            }

            if (Weights.ShouldDeserialize) {
                Weights.Deserialize(reader, serializedFile, options);
            }

            if (NormalSigns.ShouldDeserialize) {
                NormalSigns.Deserialize(reader, serializedFile, options);
            }

            if (TangentSigns.ShouldDeserialize) {
                TangentSigns.Deserialize(reader, serializedFile, options);
            }

            if (FloatColors.ShouldDeserialize) {
                FloatColors.Deserialize(reader, serializedFile, options);
            }

            if (BoneIndices.ShouldDeserialize) {
                BoneIndices.Deserialize(reader, serializedFile, options);
            }

            if (Triangles.ShouldDeserialize) {
                Triangles.Deserialize(reader, serializedFile, options);
            }
        }
    }
}
