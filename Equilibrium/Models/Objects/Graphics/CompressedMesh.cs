using System;
using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Models.Objects.Math;
using Equilibrium.Options;
using JetBrains.Annotations;

namespace Equilibrium.Models.Objects.Graphics {
    [PublicAPI]
    public record CompressedMesh(
        PackedBitInfo Vertices,
        PackedBitInfo UVs,
        PackedBitInfo Normals,
        PackedBitInfo Tangents,
        PackedBitInfo Weights,
        PackedBitInfo NormalSigns,
        PackedBitInfo TangentSigns,
        PackedBitInfo FloatColors,
        PackedBitInfo BoneIndices,
        PackedBitInfo Triangles,
        uint UVInfo) {
        public static CompressedMesh Default { get; } = new(
            PackedBitInfo.Default,
            PackedBitInfo.Default,
            PackedBitInfo.Default,
            PackedBitInfo.Default,
            PackedBitInfo.Default,
            PackedBitInfo.Default,
            PackedBitInfo.Default,
            PackedBitInfo.Default,
            PackedBitInfo.Default,
            PackedBitInfo.Default,
            0);

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

        public static CompressedMesh FromReader(BiEndianBinaryReader reader, SerializedFile file) => throw new NotImplementedException();

        public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
            throw new NotImplementedException();
        }

        public void Deserialize(BiEndianBinaryReader reader, ObjectDeserializationOptions options) {
            throw new NotImplementedException();
        }
    }
}
