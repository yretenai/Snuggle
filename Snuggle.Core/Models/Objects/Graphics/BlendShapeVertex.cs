using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models.Objects.Math;

namespace Snuggle.Core.Models.Objects.Graphics;

public record BlendShapeVertex(Vector3 Vertex, Vector3 Normal, Vector3 Tangent, uint Index) {
    public static BlendShapeVertex Default { get; } = new(Vector3.Zero, Vector3.Zero, Vector3.Zero, 0);

    public static BlendShapeVertex FromReader(BiEndianBinaryReader reader, SerializedFile file) {
        var vectors = reader.ReadArray<Vector3>(3);
        var index = reader.ReadUInt32();
        return new BlendShapeVertex(vectors[0], vectors[1], vectors[2], index);
    }

    public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
        writer.Write(Vertex.X);
        writer.Write(Vertex.Y);
        writer.Write(Vertex.Z);
        writer.Write(Normal.X);
        writer.Write(Normal.Y);
        writer.Write(Normal.Z);
        writer.Write(Tangent.X);
        writer.Write(Tangent.Y);
        writer.Write(Tangent.Z);
        writer.Write(Index);
    }
}
