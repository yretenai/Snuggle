using System;
using Equilibrium.IO;
using Equilibrium.Models.Serialization;
using JetBrains.Annotations;

namespace Equilibrium.Implementations {
    [PublicAPI, UsedImplicitly]
    public class NamedObject : SerializedObject {
        public NamedObject(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) => Name = reader.ReadString32();

        public string Name { get; init; }
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Name);

        public override string ToString() => string.IsNullOrWhiteSpace(Name) ? base.ToString() : Name;
    }
}
