using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using Snuggle.Core.Interfaces;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models;
using Snuggle.Core.Models.Serialization;
using Snuggle.Core.Options;

namespace Snuggle.Core.Implementations;

[ObjectImplementation(UnityClassId.Object)]
public class SerializedObject : IEquatable<SerializedObject>, ISerialized, ISerializedObject {
    // ReSharper disable once UnusedParameter.Local
    public SerializedObject(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : this(info, serializedFile) => IsMutated = false;

    public SerializedObject(UnityObjectInfo info, SerializedFile serializedFile) {
        SerializedFile = serializedFile;
        PathId = info.PathId;
        ClassId = info.ClassId;
        Size = info.Size;
        Info = info;
        IsMutated = true;
    }

    [JsonIgnore]
    public UnityObjectInfo Info { get; set; }

    private bool ShouldDeserializeExtraContainers => ExtraContainers.Values.Any(x => (x as ISerialized)?.ShouldDeserialize == true);

    public bool Equals(SerializedObject? other) {
        if (ReferenceEquals(null, other)) {
            return false;
        }

        if (ReferenceEquals(this, other)) {
            return true;
        }

        return PathId == other.PathId && ClassId == other.ClassId;
    }

    [JsonIgnore]
    public virtual bool ShouldDeserialize => ShouldDeserializeExtraContainers;

    public virtual void Deserialize(BiEndianBinaryReader reader, ObjectDeserializationOptions options) {
        foreach (var (_, extraContainer) in ExtraContainers) {
            if (extraContainer is not ISerialized { ShouldDeserialize: true } serialized) {
                continue;
            }

            serialized.Deserialize(reader, options);
        }
    }

    public virtual void Serialize(BiEndianBinaryWriter writer, AssetSerializationOptions options) {
        IsMutated = false;
    }

    public virtual void Free() {
        foreach (ISerialized container in ExtraContainers.Values) {
            container.Free();
        }
    }

    public void Deserialize(ObjectDeserializationOptions options) {
        if (!ShouldDeserialize) {
            return;
        }

        using var reader = new BiEndianBinaryReader(SerializedFile.OpenFile(Info), SerializedFile.Header.IsBigEndian);
        Deserialize(reader, options);
    }

    public long PathId { get; init; }

    [JsonIgnore]
    public long Size { get; set; }

    public object ClassId { get; init; }

    [JsonIgnore]
    public SerializedFile SerializedFile { get; init; }

    [JsonIgnore]
    public bool IsMutated { get; set; }

    [JsonIgnore]
    public string ObjectComparableName => ToString();

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string ObjectContainerPath { get; set; } = string.Empty;

    public Dictionary<object, object> ExtraContainers { get; } = new();

    [JsonIgnore]
    public bool NeedsLoad { get; init; }

    [JsonIgnore]
    public bool HasContainerPath => !string.IsNullOrWhiteSpace(ObjectContainerPath);

    public T GetExtraContainer<T>(object classId) where T : ISerialized, new() {
        if (!ExtraContainers.TryGetValue(classId, out var instance) || instance is not T tInstance) {
            tInstance = new T();
            ExtraContainers[classId] = tInstance;
        }

        return tInstance;
    }

    public bool TryGetExtraContainer(object classId, [MaybeNullWhen(false)] out ISerialized container) {
        if (!ExtraContainers.TryGetValue(classId, out var instance)) {
            container = null;
            return false;
        }

        container = (ISerialized) instance;
        return true;
    }

    public override string ToString() => string.IsNullOrWhiteSpace(ObjectContainerPath) ? Enum.Format(ClassId.GetType(), ClassId, "G") : Path.GetFileNameWithoutExtension(ObjectContainerPath);

    public (long, string) GetCompositeId() => (PathId, SerializedFile.Name);

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
