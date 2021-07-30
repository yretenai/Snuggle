using System;
using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Meta.Options;
using Equilibrium.Models;
using Equilibrium.Models.Serialization;
using JetBrains.Annotations;

namespace Equilibrium.Implementations {
    [PublicAPI, UsedImplicitly, ObjectImplementation(UnityClassId.MonoBehaviour)]
    public class MonoBehaviour : Behaviour {
        public MonoBehaviour(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) {
            Script = PPtr<MonoScript>.FromReader(reader, serializedFile);
            Name = reader.ReadString32();
            ShouldDeserialize = true;
        }

        public MonoBehaviour(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) {
            Script = PPtr<MonoScript>.Null;
            Name = string.Empty;
        }

        public PPtr<MonoScript> Script { get; set; }
        public string Name { get; set; }
        public object? Data { get; set; }
        public object? SerializationInfo { get; set; }

        public override void Deserialize(BiEndianBinaryReader reader) {
            base.Deserialize(reader);
            throw new NotImplementedException();
        }

        public override void Serialize(BiEndianBinaryWriter writer, UnityVersion? targetVersion, FileSerializationOptions options) {
            if (ShouldDeserialize) {
                throw new InvalidOperationException();
            }

            base.Serialize(writer, targetVersion, options);

            Script.ToWriter(writer, SerializedFile, targetVersion);
            writer.WriteString32(Name);
            throw new NotImplementedException();
        }

        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Script, Name);

        public override string ToString() => string.IsNullOrEmpty(Name) ? base.ToString() : Name;
    }
}
