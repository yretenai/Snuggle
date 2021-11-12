using JetBrains.Annotations;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;

namespace Snuggle.Core.Models.Objects.Graphics;

[PublicAPI]
public record MeshBlendShapeChannel(
    string Name,
    uint Hash,
    int Index,
    int Count) {
    public static MeshBlendShapeChannel Default { get; } = new(string.Empty, 0, 0, 0);

    public static MeshBlendShapeChannel FromReader(BiEndianBinaryReader reader, SerializedFile file) {
        var name = reader.ReadString32();
        var hash = reader.ReadUInt32();
        var index = reader.ReadInt32();
        var count = reader.ReadInt32();
        return new MeshBlendShapeChannel(name, hash, index, count);
    }

    public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
        writer.WriteString32(Name);
        writer.Write(Hash);
        writer.Write(Index);
        writer.Write(Count);
    }
}
