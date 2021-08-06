using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

        [JsonIgnore]
        public long Size { get; set; }

        public object ClassId { get; init; }

        [JsonIgnore]
        public SerializedFile SerializedFile { get; init; }

        [JsonIgnore]
        public virtual bool ShouldDeserialize { get; }

        [JsonIgnore]
        public bool IsMutated { get; set; }

        [JsonIgnore]
        public string ObjectComparableName => ToString();

        [JsonIgnore]
        public string ObjectContainerPath { get; set; } = string.Empty;

        public Dictionary<object, object> ExtraContainers { get; } = new();
        public bool NeedsLoad { get; set; }

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

        public virtual void Serialize(BiEndianBinaryWriter writer, AssetSerializationOptions options) {
            IsMutated = false;
        }

        public virtual void Free() {
            foreach (ISerialized container in ExtraContainers.Values) {
                container.Free();
            }
        }

        public void Deserialize(ObjectDeserializationOptions options) {
            using var reader = new BiEndianBinaryReader(SerializedFile.OpenFile(PathId), SerializedFile.Header.IsBigEndian);
            Deserialize(reader, options);
        }

        protected T GetExtraContainer<T>(object classId) where T : ISerialized, new() {
            if (!ExtraContainers.TryGetValue(classId, out var instance) ||
                instance is not T tInstance) {
                tInstance = new T();
                ExtraContainers[classId] = tInstance;
            }

            return tInstance;
        }

        protected bool TryGetExtraContainer(object classId, [MaybeNullWhen(false)] out ISerialized? container) {
            if (!ExtraContainers.TryGetValue(classId, out var instance)) {
                container = null;
                return false;
            }

            container = (ISerialized) instance;
            return true;
        }

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
