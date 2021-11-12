using System;
using JetBrains.Annotations;
using Snuggle.Core.IO;
using Snuggle.Core.Models.Serialization;
using Snuggle.Core.Options;

namespace Snuggle.Core.Implementations; 

[PublicAPI, UsedImplicitly]
public class NamedObject : SerializedObject {
    public NamedObject(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) => Name = reader.ReadString32();
    public NamedObject(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) => Name = string.Empty;

    public string Name { get; set; }
    public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Name);

    public override void Serialize(BiEndianBinaryWriter writer, AssetSerializationOptions options) {
        base.Serialize(writer, options);
        writer.WriteString32(Name);
    }

    public override string ToString() => string.IsNullOrWhiteSpace(Name) ? base.ToString() : Name;
}