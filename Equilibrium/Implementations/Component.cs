using System;
using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Meta.Options;
using Equilibrium.Models;
using Equilibrium.Models.Serialization;
using JetBrains.Annotations;

namespace Equilibrium.Implementations {
    [PublicAPI, UsedImplicitly, ObjectImplementation(UnityClassId.Component)]
    public class Component : SerializedObject {
        public Component(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) => GameObject = PPtr<GameObject>.FromReader(reader, serializedFile);
        public Component(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) => GameObject = PPtr<GameObject>.Null;

        public PPtr<GameObject> GameObject { get; set; }

        public override void Serialize(BiEndianBinaryWriter writer, UnityVersion? targetVersion, FileSerializationOptions options) {
            base.Serialize(writer, targetVersion, options);
            GameObject.ToWriter(writer, SerializedFile, targetVersion ?? SerializedFile.Version);
        }

        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), GameObject);
    }
}
