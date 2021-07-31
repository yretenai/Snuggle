using System.Collections.Generic;
using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Meta.Options;
using Equilibrium.Models;
using Equilibrium.Models.Objects;
using Equilibrium.Models.Serialization;
using JetBrains.Annotations;

namespace Equilibrium.Implementations {
    [PublicAPI, UsedImplicitly, ObjectImplementation(UnityClassId.AssetBundleManifest)]
    public class AssetBundleManifest : NamedObject {
        public AssetBundleManifest(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) {
            var assetBundleNameCount = reader.ReadInt32();
            AssetBundleNames = new Dictionary<int, string>();
            for (var i = 0; i < assetBundleNameCount; ++i) {
                AssetBundleNames[reader.ReadInt32()] = reader.ReadString32();
            }

            var assetBundlesWithVariantCount = reader.ReadInt32();
            AssetBundlesWithVariant = new List<int>(reader.ReadArray<int>(assetBundlesWithVariantCount).ToArray());

            var assetBundleInfoCount = reader.ReadInt32();
            AssetBundleInfos = new Dictionary<int, AssetBundleInfo>();
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

        public override void Serialize(BiEndianBinaryWriter writer, string fileName, UnityVersion? targetVersion, FileSerializationOptions options) {
            base.Serialize(writer, fileName, targetVersion, options);
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
                assetBundleInfo.ToWriter(writer, SerializedFile, targetVersion ?? SerializedFile.Version);
            }
        }
    }
}
