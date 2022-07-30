using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models;
using Snuggle.Core.Models.Objects.Graphics;
using Snuggle.Core.Models.Serialization;
using Snuggle.Core.Options;

namespace Snuggle.Core.Implementations;

[ObjectImplementation(UnityClassId.Material)]
public class Material : NamedObject {
    public Material(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) {
        Shader = PPtr<SerializedObject>.FromReader(reader, SerializedFile);
        ShaderKeywords = reader.ReadString32();
        LightmapFlags = reader.ReadUInt32();

        if (SerializedFile.Version >= UnityVersionRegister.Unity5_6) {
            EnableInstancingVariants = reader.ReadBoolean();
        }

        if (SerializedFile.Version >= UnityVersionRegister.Unity2017) {
            DoubleSidedGI = reader.ReadBoolean();
        }

        reader.Align();

        CustomRenderQueue = reader.ReadInt32();

        if (SerializedFile.Version >= UnityVersionRegister.Unity5_1) {
            var tagMapCount = reader.ReadInt32();
            StringTagMap.EnsureCapacity(tagMapCount);
            for (var i = 0; i < tagMapCount; ++i) {
                StringTagMap[reader.ReadString32()] = reader.ReadString32();
            }
        }

        if (SerializedFile.Version >= UnityVersionRegister.Unity5_6) {
            var disabledPassesCount = reader.ReadInt32();
            DisabledShaderPasses.EnsureCapacity(disabledPassesCount);
            for (var i = 0; i < disabledPassesCount; ++i) {
                DisabledShaderPasses.Add(reader.ReadString32());
            }
        }

        SavedPropertiesStart = reader.BaseStream.Position;
        UnityPropertySheet.Seek(reader, SerializedFile);

        if (SerializedFile.Version >= UnityVersionRegister.Unity2020) {
            var buildStacksCount = reader.ReadInt32();
            BuildTextureStacks.EnsureCapacity(buildStacksCount);
            for (var i = 0; i < buildStacksCount; ++i) {
                BuildTextureStacks.Add(BuildTextureStackReference.FromReader(reader, SerializedFile));
            }
        }
    }

    public Material(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) {
        Shader = PPtr<SerializedObject>.Null;
        ShaderKeywords = string.Empty;
        SavedProperties = UnityPropertySheet.Default;
    }

    public PPtr<SerializedObject> Shader { get; set; }
    public string ShaderKeywords { get; set; }
    public uint LightmapFlags { get; set; }
    public bool EnableInstancingVariants { get; set; }
    public bool DoubleSidedGI { get; set; }
    public int CustomRenderQueue { get; set; }
    public Dictionary<string, string> StringTagMap { get; set; } = new();
    public List<string> DisabledShaderPasses { get; set; } = new();
    public UnityPropertySheet? SavedProperties { get; set; }
    public List<BuildTextureStackReference> BuildTextureStacks { get; set; } = new();
    private long SavedPropertiesStart { get; } = -1;

    private bool ShouldDeserializeSavedProperties => SavedPropertiesStart > -1 && SavedProperties == null;

    [JsonIgnore]
    public override bool ShouldDeserialize => base.ShouldDeserialize || ShouldDeserializeSavedProperties;

    public override void Deserialize(BiEndianBinaryReader reader, ObjectDeserializationOptions options) {
        base.Deserialize(reader, options);

        if (ShouldDeserializeSavedProperties) {
            reader.BaseStream.Position = SavedPropertiesStart;
            SavedProperties = UnityPropertySheet.FromReader(reader, SerializedFile);
        }
    }

    public override void Serialize(BiEndianBinaryWriter writer, AssetSerializationOptions options) {
        throw new NotImplementedException();
    }
}
