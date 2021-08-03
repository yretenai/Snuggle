using System;
using System.Numerics;
using Equilibrium.IO;
using Equilibrium.Meta;
using JetBrains.Annotations;

namespace Equilibrium.Models.Objects.Graphics {
    [PublicAPI]
    public record BlendShapeVertex(
        Vector3 Vertex,
        Vector3 Normal,
        Vector3 Tangent,
        uint Index) {
        public static BlendShapeVertex Default { get; } = new(Vector3.Zero, Vector3.Zero, Vector3.Zero, 0);

        public static BlendShapeVertex FromReader(BiEndianBinaryReader reader, SerializedFile file) => throw new NotImplementedException();

        public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
            throw new NotImplementedException();
        }
    }
}
