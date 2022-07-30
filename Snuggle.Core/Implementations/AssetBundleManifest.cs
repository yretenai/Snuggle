using System;
using System.Collections.Generic;
using Snuggle.Core.Extensions;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models;
using Snuggle.Core.Models.Objects;
using Snuggle.Core.Models.Serialization;
using Snuggle.Core.Options;

namespace Snuggle.Core.Implementations;

[ObjectImplementation(UnityClassId.AssetBundleManifest)]
public class AssetBundleManifest : NamedObject {
    public AssetBundleManifest(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) {
        var assetBundleNameCount = reader.ReadInt32();
        AssetBundleNames.EnsureCapacity(assetBundleNameCount);
        for (var i = 0; i < assetBundleNameCount; ++i) {
            AssetBundleNames[reader.ReadInt32()] = reader.ReadString32();
        }

        var assetBundlesWithVariantCount = reader.ReadInt32();
        AssetBundlesWithVariant.AddRange(reader.ReadSpan<int>(assetBundlesWithVariantCount));

        var assetBundleInfoCount = reader.ReadInt32();
        AssetBundleInfos.EnsureCapacity(assetBundleInfoCount);
        for (var i = 0; i < assetBundleInfoCount; ++i) {
            AssetBundleInfos[reader.ReadInt32()] = AssetBundleInfo.FromReader(reader, serializedFile);
        }
    }

    public AssetBundleManifest(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) { }

    public Dictionary<int, string> AssetBundleNames { get; set; } = new();
    public List<int> AssetBundlesWithVariant { get; set; } = new();
    public Dictionary<int, AssetBundleInfo> AssetBundleInfos { get; set; } = new();

    public override void Serialize(BiEndianBinaryWriter writer, AssetSerializationOptions options) {
        base.Serialize(writer, options);
        writer.Write(AssetBundleNames.Count);
        foreach (var (id, name) in AssetBundleNames) {
            writer.Write(id);
            writer.Write(name);
        }

        writer.WriteArray(AssetBundlesWithVariant.ToArray());

        writer.Write(AssetBundleInfos.Count);
        foreach (var (id, assetBundleInfo) in AssetBundleInfos) {
            writer.Write(id);
            assetBundleInfo.ToWriter(writer, SerializedFile, options.TargetVersion);
        }
    }

    public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), AssetBundleNames, AssetBundleInfos, AssetBundlesWithVariant);
}
