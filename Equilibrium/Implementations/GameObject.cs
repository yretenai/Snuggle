using System;
using System.Collections.Generic;
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
            Components = new List<(ClassId, PPtr<Component>)>();
            if (serializedFile.Version < new UnityVersion(5, 5)) {
                for (var i = 0; i < componentCount; ++i) {
                    Components.Add(((ClassId) reader.ReadInt32(), PPtr<Component>.FromReader(reader, serializedFile)));
                }
            } else {
                for (var i = 0; i < componentCount; ++i) {
                    Components.Add((ClassId.Unknown, PPtr<Component>.FromReader(reader, serializedFile)));
                }
            }

            Layer = reader.ReadUInt32();
            Name = reader.ReadString32();
            Tag = reader.ReadUInt16();
            Active = reader.ReadBoolean();
            reader.Align();
        }

        public GameObject(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) {
            Components = new List<(ClassId ClassId, PPtr<Component> Ptr)>();
            Name = string.Empty;
        }

        public List<(ClassId ClassId, PPtr<Component> Ptr)> Components { get; set; }
        public uint Layer { get; set; }
        public string Name { get; set; }
        public ushort Tag { get; set; }
        public bool Active { get; set; }

        public override void Serialize(BiEndianBinaryWriter writer) {
            base.Serialize(writer);
            writer.Write(Components.Count);
            if (SerializedFile.Version < new UnityVersion(5, 5)) {
                foreach (var (classId, ptr) in Components) {
                    writer.Write((int) classId);
                    ptr.ToWriter(writer, SerializedFile);
                }
            } else {
                foreach (var (_, ptr) in Components) {
                    ptr.ToWriter(writer, SerializedFile);
                }
            }

            writer.Write(Layer);
            writer.WriteString32(Name);
            writer.Write(Tag);
            writer.Write(Active);
            writer.Align();
        }

        public override int GetHashCode() => HashCode.Combine(Components, Name, Active);
        public override string ToString() => string.IsNullOrEmpty(Name) ? base.ToString() : Name;
    }
}
