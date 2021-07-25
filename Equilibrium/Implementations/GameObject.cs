using System;
using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Models;
using Equilibrium.Models.Serialization;
using JetBrains.Annotations;

namespace Equilibrium.Implementations {
    [PublicAPI, UsedImplicitly, ObjectImplementation(ClassId.GameObject)]
    public class GameObject : SerializedObject {
        public GameObject(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) {
            var componentCount = reader.ReadInt32();
            Components = new (ClassId, PPtr<Component>)[componentCount];
            if (serializedFile.Version < new UnityVersion(5, 5)) {
                for (var i = 0; i < componentCount; ++i) {
                    Components[i] = ((ClassId) reader.ReadInt32(), PPtr<Component>.FromReader(reader, serializedFile));
                }
            } else {
                for (var i = 0; i < componentCount; ++i) {
                    Components[i] = (ClassId.Unknown, PPtr<Component>.FromReader(reader, serializedFile));
                }
            }

            Layer = reader.ReadUInt32();
            Name = reader.ReadString32();
            Tag = reader.ReadUInt16();
            Active = reader.ReadBoolean();
            reader.Align();
        }

        public (ClassId ClassId, PPtr<Component> Ptr)[] Components { get; init; }
        public uint Layer { get; init; }
        public string Name { get; init; }
        public ushort Tag { get; init; }
        public bool Active { get; init; }

        public override int GetHashCode() => HashCode.Combine(Components, Name, Active);
        public override string ToString() => string.IsNullOrEmpty(Name) ? base.ToString() : Name;
    }
}
