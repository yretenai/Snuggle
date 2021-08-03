using System;
using Equilibrium.IO;
using Equilibrium.Meta;
using JetBrains.Annotations;

namespace Equilibrium.Models.Objects.Graphics {
    [PublicAPI]
    public record MeshBlendShape(
        uint FirstVertex,
        uint VertexCount,
        bool HasNormals,
        bool HasTangents) {
        public static MeshBlendShape Default { get; } = new(0, 0, false, false);

        public static MeshBlendShape FromReader(BiEndianBinaryReader reader, SerializedFile file) => throw new NotImplementedException();

        public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
            throw new NotImplementedException();
        }
    }
}
