using System;
using System.Text.Json.Serialization;
using Equilibrium.Interfaces;
using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Models;
using Equilibrium.Models.Serialization;
using Equilibrium.Options;
using JetBrains.Annotations;

namespace Equilibrium.Implementations {
    [PublicAPI, UsedImplicitly, ObjectImplementation(UnityClassId.Object)]
    public class SerializedObject : IEquatable<SerializedObject>, ISerialized {
        public SerializedObject(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : this(info, serializedFile) {
            IsMutated = false;
            Size = info.Size;
        }

        public SerializedObject(UnityObjectInfo info, SerializedFile serializedFile) {
            SerializedFile = serializedFile;
            PathId = info.PathId;
            ClassId = info.ClassId;
            Size = info.Size;
            IsMutated = true;
        }

        public long PathId { get; init; }
        public long Size { get; set; }
        public object ClassId { get; init; }

        [JsonIgnore]
        public SerializedFile SerializedFile { get; init; }

        [JsonIgnore]
        public virtual bool ShouldDeserialize { get; }

        [JsonIgnore]
        public bool IsMutated { get; set; }

        public string ObjectComparableName => ToString();
        public string ObjectContainerPath { get; set; } = string.Empty;

        public bool Equals(SerializedObject? other) {
            if (ReferenceEquals(null, other)) {
                return false;
            }

            if (ReferenceEquals(this, other)) {
                return true;
            }

            return PathId == other.PathId && ClassId == other.ClassId;
        }

        public virtual void Deserialize(BiEndianBinaryReader reader, ObjectDeserializationOptions options) { }

        public void Deserialize(ObjectDeserializationOptions options) {
            using var reader = new BiEndianBinaryReader(SerializedFile.OpenFile(PathId), SerializedFile.Header.IsBigEndian);
            Deserialize(reader, options);
        }

        public virtual void Serialize(BiEndianBinaryWriter writer, string fileName, UnityVersion targetVersion, FileSerializationOptions options) {
            IsMutated = false;
        }

        public virtual void Free() { }

        public override string ToString() => Enum.Format(ClassId.GetType(), ClassId, "G");

        public override bool Equals(object? obj) {
            if (ReferenceEquals(null, obj)) {
                return false;
            }

            if (ReferenceEquals(this, obj)) {
                return true;
            }

            return obj is SerializedObject unityObject && Equals(unityObject);
        }

        public override int GetHashCode() => HashCode.Combine(ClassId, PathId);
    }
}
