using System;
using System.Collections.Generic;
using DragonLib;
using JetBrains.Annotations;
using Snuggle.Core.IO;
using Snuggle.Core.Options;

namespace Snuggle.Core.Models.Serialization;

[PublicAPI]
public record UnityTypeTreeNode(
    int Version,
    int Level,
    UnityTypeArrayKind ArrayKind,
    uint TypeOffset,
    uint NameOffset,
    int Size,
    int Index,
    UnityTypeTreeFlags Flags,
    int VariableCount,
    ulong TypeHash,
    string Type,
    string Name) {
    private const string StaticStringBuffer = "AABB\0AnimationClip\0AnimationCurve\0AnimationState\0Array\0Base\0BitField\0bitset\0bool\0char\0ColorRGBA\0Component\0data\0deque\0double\0dynamic_array\0FastPropertyName\0first\0float\0Font\0GameObject\0Generic Mono\0GradientNEW\0GUID\0GUIStyle\0int\0list\0long long\0map\0Matrix4x4f\0MdFour\0MonoBehaviour\0MonoScript\0m_ByteSize\0m_Curve\0m_EditorClassIdentifier\0m_EditorHideFlags\0m_Enabled\0m_ExtensionPtr\0m_GameObject\0m_Index\0m_IsArray\0m_IsStatic\0m_MetaFlag\0m_Name\0m_ObjectHideFlags\0m_PrefabInternal\0m_PrefabParentObject\0m_Script\0m_StaticEditorFlags\0m_Type\0m_Version\0Object\0pair\0PPtr<Component>\0PPtr<GameObject>\0PPtr<Material>\0PPtr<MonoBehaviour>\0PPtr<MonoScript>\0PPtr<Object>\0PPtr<Prefab>\0PPtr<Sprite>\0PPtr<TextAsset>\0PPtr<Texture>\0PPtr<Texture2D>\0PPtr<Transform>\0Prefab\0Quaternionf\0Rectf\0RectInt\0RectOffset\0second\0set\0short\0size\0SInt16\0SInt32\0SInt64\0SInt8\0staticvector\0string\0TextAsset\0TextMesh\0Texture\0Texture2D\0Transform\0TypelessData\0UInt16\0UInt32\0UInt64\0UInt8\0unsigned int\0unsigned long long\0unsigned short\0vector\0Vector2f\0Vector3f\0Vector4f\0m_ScriptingClassIdentifier\0Gradient\0Type*\0int2_storage\0int3_storage\0BoundsInt\0m_CorrespondingSourceObject\0m_PrefabInstance\0m_PrefabAsset\0FileSize\0Hash128";

    internal static Memory<byte> StaticBuffer { get; } = new(StaticStringBuffer.ToSpan().ToArray());

    public static UnityTypeTreeNode FromReader(BiEndianBinaryReader reader, UnitySerializedFile header, SnuggleCoreOptions options) {
        var version = reader.ReadInt16();
        var level = reader.ReadByte();
        var arrayKind = (UnityTypeArrayKind) reader.ReadByte();
        var typeOffset = reader.ReadUInt32();
        var nameOffset = reader.ReadUInt32();
        var size = reader.ReadInt32();
        var index = reader.ReadInt32();
        var flags = (UnityTypeTreeFlags) reader.ReadUInt32();
        var typeHash = 0ul;
        if (header.FileVersion >= UnitySerializedFileVersion.RefObject) {
            typeHash = reader.ReadUInt64();
        }

        return new UnityTypeTreeNode(
            version,
            level,
            arrayKind,
            typeOffset,
            nameOffset,
            size,
            index,
            flags,
            0,
            typeHash,
            string.Empty,
            string.Empty);
    }

    public static UnityTypeTreeNode FromReaderLegacy(BiEndianBinaryReader reader, UnitySerializedFile header, SnuggleCoreOptions options) {
        var type = reader.ReadNullString();
        var name = reader.ReadNullString();
        var size = reader.ReadInt32();
        var variableCount = 0;
        if (header.FileVersion == UnitySerializedFileVersion.VariableCount) {
            variableCount = reader.ReadInt32();
        }

        var index = 0;
        if (header.FileVersion >= UnitySerializedFileVersion.TypeTreeIndex) {
            index = reader.ReadInt32();
        }

        var flags = (UnityTypeArrayKind) reader.ReadInt32();
        var version = reader.ReadInt32();
        var metaFlags = (UnityTypeTreeFlags) 0;
        if (header.FileVersion >= UnitySerializedFileVersion.TypeTreeMeta) {
            metaFlags = (UnityTypeTreeFlags) reader.ReadInt32();
        }

        return new UnityTypeTreeNode(
            version,
            0,
            flags,
            0,
            0,
            size,
            index,
            metaFlags,
            variableCount,
            0,
            type,
            name);
    }

    // ReSharper disable once FunctionRecursiveOnAllPaths
    public static UnityTypeTreeNode[] ArrayFromReaderLegacy(BiEndianBinaryReader reader, UnitySerializedFile header, SnuggleCoreOptions options, int count, int level) {
        var list = new List<UnityTypeTreeNode>();
        for (var i = 0; i < count; ++i) {
            list.Add(FromReaderLegacy(reader, header, options) with { Level = level });
        }

        var childCount = reader.ReadInt32();
        list.AddRange(ArrayFromReaderLegacy(reader, header, options, childCount, level + 1));
        return list.ToArray();
    }

    public static string GetString(uint offset, Span<byte> buffer) {
        var safeOffset = (int) (offset & 0x7FFFFFFF);
        if (safeOffset >= buffer.Length) {
            return string.Empty;
        }

        return buffer[safeOffset..].ReadString() ?? string.Empty;
    }
}
