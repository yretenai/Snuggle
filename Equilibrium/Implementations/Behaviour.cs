using System;
using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Meta.Options;
using Equilibrium.Models;
using Equilibrium.Models.Serialization;
using JetBrains.Annotations;

namespace Equilibrium.Implementations {
    [PublicAPI, UsedImplicitly, ObjectImplementation(UnityClassId.Behaviour)]
    public class Behaviour : Component {
        public Behaviour(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) {
            Enabled = reader.ReadBoolean();
            reader.Align();
        }

        public Behaviour(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) { }

        public bool Enabled { get; set; }

        public override void Serialize(BiEndianBinaryWriter writer, UnityVersion? targetVersion, FileSerializationOptions options) {
            base.Serialize(writer, targetVersion, options);
            writer.Write(Enabled);
            writer.Align();
        }

        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Enabled);
    }
}
