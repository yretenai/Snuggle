using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Snuggle.Core.Exceptions;
using Snuggle.Core.Game.Unite;
using Snuggle.Core.Interfaces;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models;
using Snuggle.Core.Models.Objects;
using Snuggle.Core.Models.Serialization;
using Snuggle.Core.Options;
using Snuggle.Core.Options.Game;

namespace Snuggle.Core.Implementations;

[PublicAPI]
[UsedImplicitly]
[ObjectImplementation(UnityClassId.AssetBundle)]
public class AssetBundle : NamedObject, ICABPathProvider {
    public AssetBundle(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) {
        PreloadStart = reader.BaseStream.Position;
        var preloadCount = reader.ReadInt32();
        reader.BaseStream.Seek(PPtr<SerializedObject>.Size * preloadCount, SeekOrigin.Current);

        var containerCount = reader.ReadInt32();
        Container.EnsureCapacity(containerCount);
        for (var i = 0; i < containerCount; ++i) {
            Container.Add(new KeyValuePair<string, AssetInfo>(reader.ReadString32(), AssetInfo.FromReader(reader, serializedFile)));
        }

        if (serializedFile.Version >= UnityVersionRegister.Unity5_4 && serializedFile.Version < UnityVersionRegister.Unity5_5) {
            var classInfoCount = reader.ReadInt32();
            ClassInfos.EnsureCapacity(classInfoCount);
            for (var i = 0; i < classInfoCount; ++i) {
                ClassInfos[reader.ReadInt32()] = reader.ReadUInt32();
            }
        }

        MainAsset = AssetInfo.FromReader(reader, serializedFile);
        RuntimeCompatibility = reader.ReadUInt32();

        if (serializedFile.Options.Game is UnityGame.PokemonUnite && SerializedFile.Options.GameOptions.TryGetOptionsObject<UniteOptions>(UnityGame.PokemonUnite, out var uniteOptions) && uniteOptions.GameVersion >= UniteVersion.Version1_2) {
            var container = GetExtraContainer<UniteAssetBundleExtension>(UnityClassId.AssetBundle);
            container.Unknown1 = reader.ReadUInt32();
        }

        AssetBundleName = reader.ReadString32();

        var dependencyCount = reader.ReadInt32();
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
            SceneHashes.EnsureCapacity(sceneHashCount);
            for (var i = 0; i < sceneHashCount; ++i) {
                SceneHashes[reader.ReadString32()] = reader.ReadString32();
            }
        }
    }

    public AssetBundle(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) {
        AssetBundleName = string.Empty;
        MainAsset = new AssetInfo(0, 0, PPtr<SerializedObject>.Null);
    }

    private long PreloadStart { get; set; }
    public Memory<PPtr<SerializedObject>>? PreloadTable { get; set; }
    public List<KeyValuePair<string, AssetInfo>> Container { get; set; } = new();
    public Dictionary<int, uint> ClassInfos { get; set; } = new();
    public AssetInfo MainAsset { get; set; }
    public uint RuntimeCompatibility { get; set; }
    public string AssetBundleName { get; set; }
    public List<string> Dependencies { get; set; } = new();
    public bool IsStreamedSceneAssetBundle { get; set; }
    public int ExplicitDataLayout { get; set; }
    public int PathFlags { get; set; }
    public Dictionary<string, string> SceneHashes { get; set; } = new();

    private bool ShouldDeserializePreloadTable => PreloadStart > -1 && PreloadTable == null;

    public override bool ShouldDeserialize => base.ShouldDeserialize || ShouldDeserializePreloadTable;

    public IReadOnlyDictionary<PPtr<SerializedObject>, string> GetCABPaths() {
        return Container.DistinctBy(x => x.Value.Asset).ToDictionary(x => x.Value.Asset, x => x.Key);
    }

    public override void Free() {
        if (IsMutated) {
            return;
        }

        base.Free();
        PreloadTable = null;
    }

    public override void Deserialize(BiEndianBinaryReader reader, ObjectDeserializationOptions options) {
        base.Deserialize(reader, options);

        if (ShouldDeserializePreloadTable) {
            reader.BaseStream.Seek(PreloadStart, SeekOrigin.Begin);
            var preloadCount = reader.ReadInt32();
            PreloadTable = PPtr<SerializedObject>.ArrayFromReader(reader, SerializedFile, preloadCount).ToArray();
        }
    }

    public override void Serialize(BiEndianBinaryWriter writer, AssetSerializationOptions options) {
        if (ShouldDeserialize) {
            throw new IncompleteDeserializationException();
        }

        base.Serialize(writer, options);

        writer.Write(PreloadTable!.Value.Length);
        foreach (var ptr in PreloadTable.Value.Span) {
            ptr.ToWriter(writer, SerializedFile, options.TargetVersion);
        }

        writer.Write(Container.Count);
        foreach (var (name, info) in Container) {
            writer.WriteString32(name);
            info.ToWriter(writer, SerializedFile, options.TargetVersion);
        }

        if (options.TargetVersion >= UnityVersionRegister.Unity5_4 && options.TargetVersion < UnityVersionRegister.Unity5_5) {
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
