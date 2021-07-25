using System.Collections.Generic;
using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Models;
using Equilibrium.Models.Objects;
using Equilibrium.Models.Serialization;
using JetBrains.Annotations;

namespace Equilibrium.Implementations {
    [PublicAPI, UsedImplicitly, ObjectImplementation(ClassId.AssetBundleManifest)]
    public class AssetBundleManifest : NamedObject {
        public AssetBundleManifest(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) {
            var assetBundleNameCount = reader.ReadInt32();
            AssetBundleNames = new Dictionary<int, string>(assetBundleNameCount);
            for (var i = 0; i < assetBundleNameCount; ++i) {
                AssetBundleNames[reader.ReadInt32()] = reader.ReadString32();
            }

            var assetBundlesWithVariantCount = reader.ReadInt32();
            AssetBundlesWithVariant = reader.ReadArray<int>(assetBundlesWithVariantCount).ToArray();

            var assetBundleInfoCount = reader.ReadInt32();
            AssetBundleInfos = new Dictionary<int, AssetBundleInfo>(assetBundleInfoCount);
            for (var i = 0; i < assetBundleInfoCount; ++i) {
                AssetBundleInfos[reader.ReadInt32()] = AssetBundleInfo.FromReader(reader, serializedFile);
            }
        }

        public Dictionary<int, string> AssetBundleNames { get; init; }
        public int[] AssetBundlesWithVariant { get; init; }
        public Dictionary<int, AssetBundleInfo> AssetBundleInfos { get; init; }
    }
}
