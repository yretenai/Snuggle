using System;
using System.Collections.Generic;
using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Meta.Options;
using Equilibrium.Models;
using Equilibrium.Models.Objects;
using Equilibrium.Models.Serialization;
using JetBrains.Annotations;

namespace Equilibrium.Implementations {
    [PublicAPI, UsedImplicitly, ObjectImplementation(UnityClassId.GameObject)]
    public class GameObject : SerializedObject {
        public GameObject(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) {
            var componentCount = reader.ReadInt32();
            Components = new List<ComponentPair>();
            for (var i = 0; i < componentCount; ++i) {
                Components.Add(ComponentPair.FromReader(reader, serializedFile));
            }

            Layer = reader.ReadUInt32();
            Name = reader.ReadString32();
            Tag = reader.ReadUInt16();
            Active = reader.ReadBoolean();
            reader.Align();
        }

        public GameObject(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) {
            Components = new List<ComponentPair>();
            Name = string.Empty;
        }

        public List<ComponentPair> Components { get; set; }
        public uint Layer { get; set; }
        public string Name { get; set; }
        public ushort Tag { get; set; }
        public bool Active { get; set; }

        public void CacheClassIds() {
            var components = new List<ComponentPair>();
            foreach (var componentPtr in Components) {
                if (!componentPtr.ClassId.Equals(UnityClassId.Unknown)) {
                    components.Add(componentPtr);
                    continue;
                }

                var value = componentPtr.Ptr.Info?.ClassId ?? componentPtr.ClassId;

                components.Add(new ComponentPair(value, componentPtr.Ptr));
            }

            Components = components;
        }

        public override void Serialize(BiEndianBinaryWriter writer, UnityVersion? targetVersion, FileSerializationOptions options) {
            base.Serialize(writer, targetVersion, options);
            writer.Write(Components.Count);
            if (targetVersion < new UnityVersion(5, 5)) {
                foreach (var (classId, ptr) in Components) {
                    writer.Write((int) classId);
                    ptr.ToWriter(writer, SerializedFile, targetVersion ?? SerializedFile.Version);
                }
            } else {
                foreach (var (_, ptr) in Components) {
                    ptr.ToWriter(writer, SerializedFile, targetVersion ?? SerializedFile.Version);
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
