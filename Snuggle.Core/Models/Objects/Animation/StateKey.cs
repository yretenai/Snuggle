using Snuggle.Core.IO;
using Snuggle.Core.Meta;

namespace Snuggle.Core.Models.Objects.Animation;

public record StateKey(uint Id, int Layer) {
    public static StateKey Default { get; } = new(0, 0);

    public static StateKey FromReader(BiEndianBinaryReader reader, SerializedFile file) {
        var id = reader.ReadUInt32();
        var layer = reader.ReadInt32();
        return new StateKey(id, layer);
    }

    public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
        writer.Write(Id);
        writer.Write(Layer);
    }
}
