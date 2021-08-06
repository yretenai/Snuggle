using System;
using System.Collections.Generic;
using System.Linq;
using Equilibrium.Interfaces;
using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Models;
using Equilibrium.Models.Objects;
using Equilibrium.Models.Serialization;
using Equilibrium.Options;
using JetBrains.Annotations;

namespace Equilibrium.Implementations {
    [PublicAPI, UsedImplicitly, ObjectImplementation(UnityClassId.AssetBundle)]
    public class AssetBundle : NamedObject, ICABPathProvider {
        public AssetBundle(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) {
            var preloadCount = reader.ReadInt32();
            PreloadTable = new List<PPtr<SerializedObject>>();
            PreloadTable.EnsureCapacity(preloadCount);
            for (var i = 0; i < preloadCount; ++i) {
                PreloadTable.Add(PPtr<SerializedObject>.FromReader(reader, serializedFile));
            }

            var containerCount = reader.ReadInt32();
            Container = new List<KeyValuePair<string, AssetInfo>>();
            Container.EnsureCapacity(containerCount);
            for (var i = 0; i < containerCount; ++i) {
                Container.Add(new KeyValuePair<string, AssetInfo>(reader.ReadString32(), AssetInfo.FromReader(reader, serializedFile)));
            }

            if (serializedFile.Version >= UnityVersionRegister.Unity5_4 &&
                serializedFile.Version < UnityVersionRegister.Unity5_5) {
                var classInfoCount = reader.ReadInt32();
                ClassInfos = new Dictionary<int, uint>();
                ClassInfos.EnsureCapacity(classInfoCount);
                for (var i = 0; i < classInfoCount; ++i) {
                    ClassInfos[reader.ReadInt32()] = reader.ReadUInt32();
                }
            } else {
                ClassInfos = new Dictionary<int, uint>();
            }

            MainAsset = AssetInfo.FromReader(reader, serializedFile);
            RuntimeCompatibility = reader.ReadUInt32();
            AssetBundleName = reader.ReadString32();

            var dependencyCount = reader.ReadInt32();
            Dependencies = new List<string>();
            Dependencies.EnsureCapacity(dependencyCount);
            for (var i = 0; i < dependencyCount; ++i) {
                Dependencies.Add(reader.ReadString32());
            }

            IsStreamedSceneAssetBundle = reader.ReadBoolean();
            reader.Align();

            if (serializedFile.Version > UnityVersionRegister.Unity2017_3) {
                ExplicitDataLayout = reader.ReadInt32();
            }

            if (serializedFile.Version > UnityVersionRegister.Unity2017_1) {
                PathFlags = reader.ReadInt32();
            }

            if (serializedFile.Version > UnityVersionRegister.Unity2017_3) {
                var sceneHashCount = reader.ReadInt32();
                SceneHashes = new Dictionary<string, string>();
                SceneHashes.EnsureCapacity(sceneHashCount);
                for (var i = 0; i < sceneHashCount; ++i) {
                    SceneHashes[reader.ReadString32()] = reader.ReadString32();
                }
            } else {
                SceneHashes = new Dictionary<string, string>();
            }
        }

        public AssetBundle(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) {
            PreloadTable = new List<PPtr<SerializedObject>>();
            Container = new List<KeyValuePair<string, AssetInfo>>();
            ClassInfos = new Dictionary<int, uint>();
            AssetBundleName = string.Empty;
            MainAsset = new AssetInfo(0, 0, PPtr<SerializedObject>.Null);
            Dependencies = new List<string>();
            SceneHashes = new Dictionary<string, string>();
        }

        public List<PPtr<SerializedObject>> PreloadTable { get; set; }
        public List<KeyValuePair<string, AssetInfo>> Container { get; set; }
        public Dictionary<int, uint> ClassInfos { get; set; }
        public AssetInfo MainAsset { get; set; }
        public uint RuntimeCompatibility { get; set; }
        public string AssetBundleName { get; set; }
        public List<string> Dependencies { get; set; }
        public bool IsStreamedSceneAssetBundle { get; set; }
        public int ExplicitDataLayout { get; set; }
        public int PathFlags { get; set; }
        public Dictionary<string, string> SceneHashes { get; set; }

        public Dictionary<PPtr<SerializedObject>, string> GetCABPaths() {
            return Container.DistinctBy(x => x.Value.Asset).ToDictionary(x => x.Value.Asset, x => x.Key);
        }

        public override void Serialize(BiEndianBinaryWriter writer, AssetSerializationOptions options) {
            base.Serialize(writer, options);

            writer.Write(PreloadTable.Count);
            foreach (var ptr in PreloadTable) {
                ptr.ToWriter(writer, SerializedFile, options.TargetVersion);
            }

            writer.Write(Container.Count);
            foreach (var (name, info) in Container) {
                writer.WriteString32(name);
                info.ToWriter(writer, SerializedFile, options.TargetVersion);
            }

            if (options.TargetVersion >= UnityVersionRegister.Unity5_4 &&
                options.TargetVersion < UnityVersionRegister.Unity5_5) {
                writer.Write(ClassInfos.Count);
                foreach (var (id, flags) in ClassInfos) {
                    writer.Write(id);
                    writer.Write(flags);
                }
            } else {
                ClassInfos = new Dictionary<int, uint>(0);
            }

            MainAsset.ToWriter(writer, SerializedFile, options.TargetVersion);
            writer.Write(RuntimeCompatibility);
            writer.WriteString32(AssetBundleName);

            writer.Write(Dependencies.Count);
            foreach (var dependency in Dependencies) {
                writer.WriteString32(dependency);
            }

            writer.Write(IsStreamedSceneAssetBundle);
            writer.Align();

            if (options.TargetVersion > UnityVersionRegister.Unity2017_3) {
                writer.Write(ExplicitDataLayout);
            }

            if (options.TargetVersion > UnityVersionRegister.Unity2017_1) {
                writer.Write(PathFlags);
            }

            if (options.TargetVersion > UnityVersionRegister.Unity2017_3) {
                writer.Write(SceneHashes.Count);
                foreach (var (scene, hash) in SceneHashes) {
                    writer.WriteString32(scene);
                    writer.WriteString32(hash);
                }
            }
        }

        public override string ToString() => string.IsNullOrWhiteSpace(AssetBundleName) ? base.ToString() : AssetBundleName;
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), AssetBundleName, MainAsset, PreloadTable.GetHashCode(), Container.GetHashCode());
    }
}
