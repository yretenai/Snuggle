using JetBrains.Annotations;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Options;

namespace Snuggle.Core.Models.Objects.Animation;

[PublicAPI]
public record ValueConstant(uint Id, uint TypeId, uint Type, uint Index) {
    public static ValueConstant FromReader(BiEndianBinaryReader reader, ObjectDeserializationOptions options) {
        var id = reader.ReadUInt32();
        var typeId = reader.ReadUInt32();
        var type = reader.ReadUInt32();
        var index = reader.ReadUInt32();
        return new ValueConstant(id, typeId, type, index);
    }

    public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
        writer.Write(Id);
        writer.Write(TypeId);
        writer.Write(Type);
        writer.Write(Index);
    }
}
