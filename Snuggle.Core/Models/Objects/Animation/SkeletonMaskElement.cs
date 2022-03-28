using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Options;

namespace Snuggle.Core.Models.Objects.Animation;

public record SkeletonMaskElement(uint PathHash, float Weight) {
    public static SkeletonMaskElement Default { get; } = new(0, 0);

    public static SkeletonMaskElement FromReader(BiEndianBinaryReader reader, ObjectDeserializationOptions options) {
        var hash = reader.ReadUInt32();
        var weight = reader.ReadSingle();
        return new SkeletonMaskElement(hash, weight);
    }

    public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
        writer.Write(PathHash);
        writer.Write(Weight);
    }
}
