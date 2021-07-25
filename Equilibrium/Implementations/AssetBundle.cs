using System;
using System.Collections.Generic;
using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Models;
using Equilibrium.Models.Objects;
using Equilibrium.Models.Serialization;
using JetBrains.Annotations;

namespace Equilibrium.Implementations {
    [PublicAPI, UsedImplicitly, ObjectImplementation(ClassId.AssetBundle)]
    public class AssetBundle : NamedObject {
        public AssetBundle(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) {
            var preloadCount = reader.ReadInt32();
            PreloadTable = new PPtr<SerializedObject>[preloadCount];
            for (var i = 0; i < preloadCount; ++i) {
                PreloadTable[i] = PPtr<SerializedObject>.FromReader(reader, serializedFile);
            }

            var containerCount = reader.ReadInt32();
            Container = new Dictionary<string, AssetInfo>(containerCount);
            for (var i = 0; i < containerCount; ++i) {
                Container[reader.ReadString32()] = AssetInfo.FromReader(reader, serializedFile);
            }

            if (serializedFile.Version >= new UnityVersion(5, 4) &&
                serializedFile.Version < new UnityVersion(5, 5)) {
                var classInfoCount = reader.ReadInt32();
                ClassInfos = new Dictionary<int, uint>();
                for (var i = 0; i < classInfoCount; ++i) {
                    ClassInfos[reader.ReadInt32()] = reader.ReadUInt32();
                }
            } else {
                ClassInfos = new Dictionary<int, uint>(0);
            }

            MainAsset = AssetInfo.FromReader(reader, serializedFile);
            RuntimeCompatibility = reader.ReadUInt32();
            AssetBundleName = reader.ReadString32();

            var dependencyCount = reader.ReadInt32();
            Dependencies = new string[dependencyCount];
            for (var i = 0; i < dependencyCount; ++i) {
                Dependencies[i] = reader.ReadString32();
            }

            IsStreamedSceneAssetBundle = reader.ReadBoolean();
            reader.Align();

            if (serializedFile.Version > new UnityVersion(2017, 3)) {
                ExplicitDataLayout = reader.ReadInt32();
            }

            if (serializedFile.Version > new UnityVersion(2017, 1)) {
                PathFlags = reader.ReadInt32();
            }

            if (serializedFile.Version > new UnityVersion(2017, 3)) {
                var sceneHashCount = reader.ReadInt32();
                SceneHashes = new Dictionary<string, string>(sceneHashCount);
                for (var i = 0; i < sceneHashCount; ++i) {
                    SceneHashes[reader.ReadString32()] = reader.ReadString32();
                }
            } else {
                SceneHashes = new Dictionary<string, string>(0);
            }
        }

        public PPtr<SerializedObject>[] PreloadTable { get; init; }
        public Dictionary<string, AssetInfo> Container { get; init; }
        public Dictionary<int, uint> ClassInfos { get; set; }
        public AssetInfo MainAsset { get; init; }
        public uint RuntimeCompatibility { get; init; }
        public string AssetBundleName { get; init; }
        public string[] Dependencies { get; init; }
        public bool IsStreamedSceneAssetBundle { get; init; }
        public int ExplicitDataLayout { get; init; }
        public int PathFlags { get; init; }
        public Dictionary<string, string> SceneHashes { get; init; }

        public override string ToString() => string.IsNullOrWhiteSpace(AssetBundleName) ? base.ToString() : AssetBundleName;
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), AssetBundleName, MainAsset);
    }
}
