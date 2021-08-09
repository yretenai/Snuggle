using System;
using System.Collections.Generic;
using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Models;
using Equilibrium.Models.Objects.Math;
using Equilibrium.Models.Serialization;
using Equilibrium.Options;
using JetBrains.Annotations;

namespace Equilibrium.Implementations {
    [PublicAPI, ObjectImplementation(UnityClassId.Transform)]
    public class Transform : Component {
        public Transform(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) {
            Rotation = reader.ReadStruct<Quaternion>();
            Translation = reader.ReadStruct<Vector3>();
            Scale = reader.ReadStruct<Vector3>();
            var childCount = reader.ReadInt32();
            Children = new List<PPtr<Transform>>();
            Children.EnsureCapacity(childCount);
            for (var i = 0; i < childCount; ++i) {
                Children.Add(PPtr<Transform>.FromReader(reader, SerializedFile));
            }

            Parent = PPtr<Transform>.FromReader(reader, SerializedFile);
        }

        public Transform(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) {
            Children = new List<PPtr<Transform>>();
            Parent = PPtr<Transform>.Null;
        }

        public Quaternion Rotation { get; set; }
        public Vector3 Translation { get; set; }
        public Vector3 Scale { get; set; }
        public List<PPtr<Transform>> Children { get; set; }
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
}
