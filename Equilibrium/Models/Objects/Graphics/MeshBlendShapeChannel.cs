using System;
using Equilibrium.IO;
using Equilibrium.Meta;
using JetBrains.Annotations;

namespace Equilibrium.Models.Objects.Graphics {
    [PublicAPI]
    public record MeshBlendShapeChannel(
        string Name,
        uint Hash,
        int Index,
        int Count) {
        public static MeshBlendShapeChannel Default { get; } = new(string.Empty, 0, 0, 0);

        public static MeshBlendShapeChannel FromReader(BiEndianBinaryReader reader, SerializedFile file) => throw new NotImplementedException();

        public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
            throw new NotImplementedException();
        }
    }
}
