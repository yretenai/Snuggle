using System;
using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Models;
using Equilibrium.Models.Serialization;
using JetBrains.Annotations;

namespace Equilibrium.Implementations {
    [PublicAPI, UsedImplicitly, ObjectImplementation(ClassId.Behaviour)]
    public class Behaviour : Component {
        public Behaviour(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) {
            Enabled = reader.ReadBoolean();
            reader.Align();
        }

        public bool Enabled { get; init; }
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Enabled);
    }
}
