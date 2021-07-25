using System;
using Equilibrium.IO;
using Equilibrium.Meta;
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

        public int ExecutionOrder { get; init; }
        public byte[] Hash { get; init; }
        public string ClassName { get; init; }
        public string Namespace { get; init; }
        public string AssemblyName { get; init; }
        public bool IsEditor { get; init; }

        public override string ToString() => $"{Namespace}.{ClassName}";
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), ClassName, Name, AssemblyName, ExecutionOrder);
    }
}
