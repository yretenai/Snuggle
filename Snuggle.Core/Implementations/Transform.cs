using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models;
using Snuggle.Core.Models.Objects.Math;
using Snuggle.Core.Models.Serialization;
using Snuggle.Core.Options;

namespace Snuggle.Core.Implementations;

[PublicAPI]
[ObjectImplementation(UnityClassId.Transform)]
public class Transform : Component {
    public Transform(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) {
        Rotation = reader.ReadStruct<Quaternion>();
        Translation = reader.ReadStruct<Vector3>();
        Scale = reader.ReadStruct<Vector3>();
        var childCount = reader.ReadInt32();
        Children.AddRange(PPtr<Transform>.ArrayFromReader(reader, SerializedFile, childCount));
        Parent = PPtr<Transform>.FromReader(reader, SerializedFile);
    }

    public Transform(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) => Parent = PPtr<Transform>.Null;

    public Quaternion Rotation { get; set; }
    public Vector3 Translation { get; set; }
    public Vector3 Scale { get; set; }
    public List<PPtr<Transform>> Children { get; set; } = new();
    public PPtr<Transform> Parent { get; set; }

    public override void Serialize(BiEndianBinaryWriter writer, AssetSerializationOptions options) {
        base.Serialize(writer, options);
        writer.WriteStruct(Rotation);
        writer.WriteStruct(Translation);
        writer.WriteStruct(Scale);
        writer.WriteStruct(Children.Count);
        foreach (var child in Children) {
            child.ToWriter(writer, SerializedFile, options.TargetVersion);
        }

        Parent.ToWriter(writer, SerializedFile, options.TargetVersion);
    }

    public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Rotation, Translation, Scale, Parent, Children);
}
