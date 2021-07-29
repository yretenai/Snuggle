using System;
using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Meta.Options;
using Equilibrium.Models;
using Equilibrium.Models.Serialization;
using JetBrains.Annotations;

namespace Equilibrium.Implementations {
    [PublicAPI, UsedImplicitly, ObjectImplementation(ClassId.MonoScript)]
    public class MonoScript : NamedObject {
        public MonoScript(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) {
            ExecutionOrder = reader.ReadInt32();
            Hash = reader.ReadBytes(16);
            ClassName = reader.ReadString32();
            Namespace = reader.ReadString32();
            AssemblyName = reader.ReadString32();

            if (serializedFile.Version < new UnityVersion(2018, 2)) {
                IsEditor = reader.ReadBoolean();
            }
        }

        public MonoScript(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) {
            Hash = new byte[16];
            ClassName = string.Empty;
            Namespace = string.Empty;
            AssemblyName = string.Empty;
        }

        public int ExecutionOrder { get; set; }
        public byte[] Hash { get; set; }
        public string ClassName { get; set; }
        public string Namespace { get; set; }
        public string AssemblyName { get; set; }
        public bool IsEditor { get; set; }

        public override void Serialize(BiEndianBinaryWriter writer, UnityVersion? targetVersion, FileSerializationOptions options) {
            base.Serialize(writer, targetVersion, options);
            writer.Write(ExecutionOrder);
            writer.Write(Hash);
            writer.WriteString32(ClassName);
            writer.WriteString32(Namespace);
            writer.WriteString32(AssemblyName);

            if (targetVersion < new UnityVersion(2018, 2)) {
                writer.Write(IsEditor);
            }
        }

        public override string ToString() => $"{Namespace}.{ClassName}";
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), ClassName, Name, AssemblyName, ExecutionOrder);
    }
}
