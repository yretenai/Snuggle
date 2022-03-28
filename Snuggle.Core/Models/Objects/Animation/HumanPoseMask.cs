using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Options;

namespace Snuggle.Core.Models.Objects.Animation;

public record HumanPoseMask(uint Word0, uint Word1) {
    public static HumanPoseMask Default { get; } = new(0, 0);

    public static HumanPoseMask FromReader(BiEndianBinaryReader reader, ObjectDeserializationOptions options) {
        var w1 = reader.ReadUInt32();
        var w2 = reader.ReadUInt32();
        return new HumanPoseMask(w1, w2);
    }

    public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
        writer.Write(Word0);
        writer.Write(Word1);
    }
}
