using System;
using JetBrains.Annotations;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models;
using Snuggle.Core.Models.Serialization;
using Snuggle.Core.Options;

namespace Snuggle.Core.Implementations; 

[PublicAPI, UsedImplicitly, ObjectImplementation(UnityClassId.Behaviour)]
public class Behaviour : Component {
    public Behaviour(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) => Enabled = reader.ReadBoolean();

    public Behaviour(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) { }

    public bool Enabled { get; set; }

    public override void Serialize(BiEndianBinaryWriter writer, AssetSerializationOptions options) {
        base.Serialize(writer, options);
        writer.Write(Enabled);
        writer.Align();
    }

    public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Enabled);
}