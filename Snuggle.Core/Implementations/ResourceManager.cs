using System.Collections.Generic;
using JetBrains.Annotations;
using Snuggle.Core.Interfaces;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models;
using Snuggle.Core.Models.Serialization;
using Snuggle.Core.Options;

namespace Snuggle.Core.Implementations;

[PublicAPI]
[UsedImplicitly]
[ObjectImplementation(UnityClassId.ResourceManager)]
public class ResourceManager : SerializedObject, ICABPathProvider {
    public ResourceManager(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) {
        var count = reader.ReadInt32();
        Container.EnsureCapacity(count);
        for (var i = 0; i < count; ++i) {
            var key = reader.ReadString32();
            var ptr = PPtr<SerializedObject>.FromReader(reader, serializedFile);
            Container[ptr] = key;
        }

        count = reader.ReadInt32();
        DependentAssets.EnsureCapacity(count);
        for (var i = 0; i < count; ++i) {
            var ptr = PPtr<SerializedObject>.FromReader(reader, serializedFile);
            var dependencyCount = reader.ReadInt32();
            var dependencies = new List<PPtr<SerializedObject>>();
            dependencies.EnsureCapacity(dependencyCount);
            dependencies.AddRange(PPtr<SerializedObject>.ArrayFromReader(reader, serializedFile, dependencyCount));
            DependentAssets[ptr] = dependencies;
        }
    }

    public ResourceManager(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) { }

    public Dictionary<PPtr<SerializedObject>, string> Container { get; set; } = new();
    public Dictionary<PPtr<SerializedObject>, List<PPtr<SerializedObject>>> DependentAssets { get; set; } = new();
    public IReadOnlyDictionary<PPtr<SerializedObject>, string> GetCABPaths() => Container;

    public override void Serialize(BiEndianBinaryWriter writer, AssetSerializationOptions options) {
        writer.Write(Container.Count);
        foreach (var (value, key) in Container) {
            writer.WriteString32(key);
            value.ToWriter(writer, SerializedFile, options.TargetVersion);
        }

        writer.Write(DependentAssets.Count);
        foreach (var (key, dependencies) in DependentAssets) {
            key.ToWriter(writer, SerializedFile, options.TargetVersion);
            writer.Write(dependencies.Count);
            foreach (var dependency in dependencies) {
                dependency.ToWriter(writer, SerializedFile, options.TargetVersion);
            }
        }
    }
}
