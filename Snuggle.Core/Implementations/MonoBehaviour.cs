using System;
using System.IO;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Snuggle.Core.Exceptions;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models;
using Snuggle.Core.Models.Serialization;
using Snuggle.Core.Options;

namespace Snuggle.Core.Implementations {
    [PublicAPI, UsedImplicitly, ObjectImplementation(UnityClassId.MonoBehaviour)]
    public class MonoBehaviour : Behaviour {
        public MonoBehaviour(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) {
            reader.Align();

            Script = PPtr<MonoScript>.FromReader(reader, serializedFile);
            Name = reader.ReadString32();
            DataStart = reader.BaseStream.Position;
        }

        public MonoBehaviour(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) {
            Script = PPtr<MonoScript>.Null;
            Name = string.Empty;
        }

        public PPtr<MonoScript> Script { get; set; }
        public string Name { get; set; }
        public object? Data { get; set; }

        [JsonIgnore]
        public ObjectNode? ObjectData { get; set; }

        private long DataStart { get; init; } = -1;

        [JsonIgnore]
        public override bool ShouldDeserialize => base.ShouldDeserialize || Data == null && !Script.IsNull;

        public override void Deserialize(BiEndianBinaryReader reader, ObjectDeserializationOptions options) {
            base.Deserialize(reader, options);

            if (ObjectData == null &&
                SerializedFile.Assets != null) {
                var script = Script.Value;
                if (script == null) {
                    throw new PPtrNullReferenceException(UnityClassId.MonoScript);
                }

                var name = script.ToString();

                var info = SerializedFile.ObjectInfos[PathId];
                if (info.TypeIndex > 0 &&
                    info.TypeIndex < SerializedFile.Types.Length &&
                    SerializedFile.Types[info.TypeIndex].TypeTree != null) {
                    ObjectData = ObjectFactory.FindObjectNode(name, SerializedFile.Types[info.TypeIndex].TypeTree, SerializedFile.Assets);
                }

                if (ObjectData == null) {
                    ObjectData = ObjectFactory.FindObjectNode(name, script, SerializedFile.Assets, options.RequestAssemblyCallback);
                }

                if (ObjectData == null) {
                    return;
                }

                reader.BaseStream.Seek(DataStart, SeekOrigin.Begin);
                Data = ObjectFactory.CreateObject(reader, ObjectData, SerializedFile, "m_Name");
            }
        }

        public override void Serialize(BiEndianBinaryWriter writer, AssetSerializationOptions options) {
            if (ShouldDeserialize) {
                throw new IncompleteDeserializationException();
            }

            base.Serialize(writer, options);

            Script.ToWriter(writer, SerializedFile, options.TargetVersion);
            writer.WriteString32(Name);
            throw new NotImplementedException();
        }

        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Script, Name, Data);

        public override string ToString() => string.IsNullOrEmpty(Name) ? base.ToString() : Name;

        public override void Free() {
            if (IsMutated) {
                return;
            }

            base.Free();
            ObjectData = null;
            Data = null;
        }
    }
}
