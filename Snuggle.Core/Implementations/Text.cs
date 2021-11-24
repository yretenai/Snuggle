using System;
using System.IO;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Snuggle.Core.Exceptions;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models;
using Snuggle.Core.Models.Serialization;
using Snuggle.Core.Options;

namespace Snuggle.Core.Implementations;

[PublicAPI]
[ObjectImplementation(UnityClassId.TextAsset)]
public class Text : NamedObject {
    public Text(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) {
        StringStart = reader.BaseStream.Position;
        var size = reader.ReadInt32();
        if (size == 0) {
            String = string.Empty;
        } else {
            reader.BaseStream.Seek(size, SeekOrigin.Current);
        }

        reader.Align();
    }

    public Text(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) { }

    private long StringStart { get; set; }
    public string? String { get; set; }

    private bool ShouldDeserializeString => StringStart > -1 && String == null;

    [JsonIgnore]
    public override bool ShouldDeserialize => base.ShouldDeserialize || ShouldDeserializeString;

    public override void Deserialize(BiEndianBinaryReader reader, ObjectDeserializationOptions options) {
        base.Deserialize(reader, options);
        reader.BaseStream.Seek(StringStart, SeekOrigin.Begin);

        if (ShouldDeserializeString) {
            String = reader.ReadString32();
        }
    }

    public override void Serialize(BiEndianBinaryWriter writer, AssetSerializationOptions options) {
        if (ShouldDeserialize) {
            throw new IncompleteDeserializationException();
        }

        base.Serialize(writer, options);
        writer.WriteString32(String!);
    }

    public override void Free() {
        base.Free();
        String = null;
    }

    public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), String);
}
