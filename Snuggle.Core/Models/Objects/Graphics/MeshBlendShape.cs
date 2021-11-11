using JetBrains.Annotations;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;

namespace Snuggle.Core.Models.Objects.Graphics {
    [PublicAPI]
    public record MeshBlendShape(
        uint FirstVertex,
        uint VertexCount,
        bool HasNormals,
        bool HasTangents) {
        public static MeshBlendShape Default { get; } = new(0, 0, false, false);

        public static MeshBlendShape FromReader(BiEndianBinaryReader reader, SerializedFile file) {
            var firstVertex = reader.ReadUInt32();
            var vertexCount = reader.ReadUInt32();
            var hasNormals = reader.ReadBoolean();
            var hasTangents = reader.ReadBoolean();
            reader.Align();
            return new MeshBlendShape(firstVertex, vertexCount, hasNormals, hasTangents);
        }

        public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
            writer.Write(FirstVertex);
            writer.Write(VertexCount);
            writer.Write(HasNormals);
            writer.Write(HasTangents);
            writer.Align();
        }
    }
}
