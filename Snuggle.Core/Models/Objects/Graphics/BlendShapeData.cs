using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using Snuggle.Core.Exceptions;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Options;

namespace Snuggle.Core.Models.Objects.Graphics;

public record BlendShapeData(List<MeshBlendShape> Shapes, List<MeshBlendShapeChannel> Channels, List<float> Weights) {
    private long VerticesStart { get; init; } = -1;

    [JsonIgnore]
    public List<BlendShapeVertex>? Vertices { get; set; }

    public static BlendShapeData Default { get; } = new(new List<MeshBlendShape>(), new List<MeshBlendShapeChannel>(), new List<float>());

    private bool ShouldDeserializeVertexData => VerticesStart > -1 && Vertices == null;

    [JsonIgnore]
    public bool ShouldDeserialize => ShouldDeserializeVertexData;

    public static BlendShapeData FromReader(BiEndianBinaryReader reader, SerializedFile file) {
        var verticesOffset = reader.BaseStream.Position;
        var verticesCount = reader.ReadInt32();
        reader.BaseStream.Seek(verticesCount * 10 * 4, SeekOrigin.Current); // Vector3 + Vector3 + Vector3 + uint

        var shapeCount = reader.ReadInt32();
        var shapes = new List<MeshBlendShape>();
        shapes.EnsureCapacity(shapeCount);
        for (var i = 0; i < shapeCount; ++i) {
            shapes.Add(MeshBlendShape.FromReader(reader, file));
        }

        var channelCount = reader.ReadInt32();
        var channels = new List<MeshBlendShapeChannel>();
        channels.EnsureCapacity(channelCount);
        for (var i = 0; i < channelCount; ++i) {
            channels.Add(MeshBlendShapeChannel.FromReader(reader, file));
        }

        var weightsCount = reader.ReadInt32();
        var weights = new List<float>();
        weights.AddRange(reader.ReadArray<float>(weightsCount));

        return new BlendShapeData(shapes, channels, weights) { VerticesStart = verticesOffset };
    }

    public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
        if (ShouldDeserialize) {
            throw new IncompleteDeserialization();
        }

        writer.Write(Vertices!.Count);
        foreach (var vertex in Vertices) {
            vertex.ToWriter(writer, serializedFile, targetVersion);
        }

        writer.Write(Shapes.Count);
        foreach (var shape in Shapes) {
            shape.ToWriter(writer, serializedFile, targetVersion);
        }

        writer.Write(Channels.Count);
        foreach (var channel in Channels) {
            channel.ToWriter(writer, serializedFile, targetVersion);
        }

        writer.WriteArray(Weights);
    }

    public void Free() {
        Vertices = null;
    }

    public void Deserialize(BiEndianBinaryReader reader, SerializedFile serializedFile, ObjectDeserializationOptions options) {
        if (ShouldDeserializeVertexData) {
            reader.BaseStream.Seek(VerticesStart, SeekOrigin.Begin);
            var verticesCount = reader.ReadInt32();
            Vertices = new List<BlendShapeVertex>();
            Vertices.EnsureCapacity(verticesCount);
            for (var i = 0; i < verticesCount; ++i) {
                Vertices.Add(BlendShapeVertex.FromReader(reader, serializedFile));
            }
        }
    }
}
