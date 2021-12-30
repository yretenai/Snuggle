using JetBrains.Annotations;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;

namespace Snuggle.Core.Models.Objects.Animation;

[PublicAPI]
public record StateRange(uint Start, uint Count) {
    public static StateRange Default { get; } = new(0, 0);

    public static StateRange FromReader(BiEndianBinaryReader reader, SerializedFile file) {
        var start = reader.ReadUInt32();
        var count = reader.ReadUInt32();
        return new StateRange(start, count);
    }

    public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
        writer.Write(Start);
        writer.Write(Count);
    }
}
