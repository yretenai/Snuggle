using System;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models;
using Snuggle.Core.Models.Serialization;
using Snuggle.Core.Options;

namespace Snuggle.Core.Implementations;

[ObjectImplementation(UnityClassId.Component)]
public class Component : SerializedObject {
    public Component(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) => GameObject = PPtr<GameObject>.FromReader(reader, serializedFile);
    public Component(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) => GameObject = PPtr<GameObject>.Null;

    public PPtr<GameObject> GameObject { get; set; }

    public override void Serialize(BiEndianBinaryWriter writer, AssetSerializationOptions options) {
        base.Serialize(writer, options);
        GameObject.ToWriter(writer, SerializedFile, options.TargetVersion);
    }

    public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), GameObject);
}
