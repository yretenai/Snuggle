using System;
using System.Collections.Generic;
using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Models;
using Equilibrium.Models.Objects;
using Equilibrium.Models.Serialization;
using Equilibrium.Options;
using JetBrains.Annotations;

namespace Equilibrium.Implementations {
    [PublicAPI, UsedImplicitly, ObjectImplementation(UnityClassId.AssetBundleManifest)]
    public class AssetBundleManifest : NamedObject {
        public AssetBundleManifest(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) {
            var assetBundleNameCount = reader.ReadInt32();
            AssetBundleNames = new Dictionary<int, string>();
            AssetBundleNames.EnsureCapacity(assetBundleNameCount);
            for (var i = 0; i < assetBundleNameCount; ++i) {
                AssetBundleNames[reader.ReadInt32()] = reader.ReadString32();
            }

            var assetBundlesWithVariantCount = reader.ReadInt32();
            AssetBundlesWithVariant = new List<int>(reader.ReadArray<int>(assetBundlesWithVariantCount).ToArray());
            AssetBundlesWithVariant.EnsureCapacity(assetBundlesWithVariantCount);

            var assetBundleInfoCount = reader.ReadInt32();
            AssetBundleInfos = new Dictionary<int, AssetBundleInfo>();
            AssetBundleInfos.EnsureCapacity(assetBundleInfoCount);
            for (var i = 0; i < assetBundleInfoCount; ++i) {
                AssetBundleInfos[reader.ReadInt32()] = AssetBundleInfo.FromReader(reader, serializedFile);
            }
        }

        public AssetBundleManifest(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) {
            AssetBundleNames = new Dictionary<int, string>();
            AssetBundlesWithVariant = new List<int>();
            AssetBundleInfos = new Dictionary<int, AssetBundleInfo>();
        }

        public Dictionary<int, string> AssetBundleNames { get; set; }
        public List<int> AssetBundlesWithVariant { get; set; }
        public Dictionary<int, AssetBundleInfo> AssetBundleInfos { get; set; }

        public override void Serialize(BiEndianBinaryWriter writer, AssetSerializationOptions options) {
            base.Serialize(writer, options);
            writer.Write(AssetBundleNames.Count);
            foreach (var (id, name) in AssetBundleNames) {
                writer.Write(id);
                writer.Write(name);
            }

            writer.Write(AssetBundlesWithVariant.Count);
            writer.WriteArray(AssetBundlesWithVariant.ToArray());

            writer.Write(AssetBundleInfos.Count);
            foreach (var (id, assetBundleInfo) in AssetBundleInfos) {
                writer.Write(id);
                assetBundleInfo.ToWriter(writer, SerializedFile, options.TargetVersion);
            }
        }

        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), AssetBundleNames, AssetBundleInfos, AssetBundlesWithVariant);
    }
}
