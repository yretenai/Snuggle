using System;
using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Models;
using Equilibrium.Models.Serialization;
using JetBrains.Annotations;

namespace Equilibrium.Implementations {
    [PublicAPI, UsedImplicitly, ObjectImplementation(ClassId.MonoBehaviour)]
    public class MonoBehaviour : Behaviour {
        public MonoBehaviour(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) {
            Script = PPtr<MonoScript>.FromReader(reader, serializedFile);
            Name = reader.ReadString32();
        }

        public PPtr<MonoScript> Script { get; init; }
        public string Name { get; init; }

        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Script, Name);

        public override string ToString() => string.IsNullOrEmpty(Name) ? base.ToString() : Name;
    }
}
