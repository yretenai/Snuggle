using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Models.Objects.Math;
using JetBrains.Annotations;

namespace Equilibrium.Models.Objects.Graphics {
    [PublicAPI]
    public record Submesh(
        uint FirstByte,
        uint IndexCount,
        GfxPrimitiveType Topology,
        uint BaseVertex,
        uint FirstVertex,
        uint VertexCount,
        AABB LocalAABB) {
        public static Submesh Default { get; } = new(0, 0, GfxPrimitiveType.Triangles, 0, 0, 0, AABB.Default);

        public static Submesh FromReader(BiEndianBinaryReader reader, SerializedFile file) {
            var firstByte = reader.ReadUInt32();
            var indexCount = reader.ReadUInt32();
            var topo = (GfxPrimitiveType) reader.ReadInt32();
            var baseVertex = 0u;
            if (file.Version >= UnityVersionRegister.Unity2017_3) {
                baseVertex = reader.ReadUInt32();
            }

            var firstVertex = reader.ReadUInt32();
            var vertexCount = reader.ReadUInt32();
            var aabb = AABB.FromReader(reader, file);
            return new Submesh(firstByte, indexCount, topo, baseVertex, firstVertex, vertexCount, aabb);
        }

        public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
            writer.Write(FirstByte);
            writer.Write(IndexCount);
            writer.Write((int) Topology);
            if (targetVersion >= UnityVersionRegister.Unity2019_3) {
                writer.Write(BaseVertex);
            }

            writer.Write(FirstVertex);
            writer.Write(VertexCount);
            LocalAABB.ToWriter(writer, serializedFile, targetVersion);
        }
    }
}
