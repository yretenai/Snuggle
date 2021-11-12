using System;
using JetBrains.Annotations;
using Snuggle.Core.IO;
using Snuggle.Core.Options;

namespace Snuggle.Core.Models.Serialization; 

[PublicAPI]
public record UnitySerializedType(
    object ClassId,
    bool IsStrippedType,
    short ScriptTypeIndex,
    UnityTypeTree? TypeTree,
    string ClassName,
    string NameSpace,
    string AssemblyName,
    int[] Dependencies) {
    public byte[] Hash { get; init; } = Array.Empty<byte>();
    public byte[] ScriptId { get; init; } = Array.Empty<byte>();

    public static UnitySerializedType FromReader(BiEndianBinaryReader reader, UnitySerializedFile header, SnuggleOptions options, bool isRef = false) {
        var classId = reader.ReadInt32();
        var classIdEx = ObjectFactory.GetClassIdForGame(options.Game, classId);
        var isStrippedType = false;
        if (header.FileVersion >= UnitySerializedFileVersion.StrippedType) {
            isStrippedType = reader.ReadBoolean();
        }

        short typeIndex = -1;
        if (header.FileVersion >= UnitySerializedFileVersion.NewTypeData) {
            typeIndex = reader.ReadInt16();
        }

        var hash = Array.Empty<byte>();
        var scriptId = Array.Empty<byte>();

        if (header.FileVersion >= UnitySerializedFileVersion.TypeTreeHash) {
            if (isRef && typeIndex >= 0) {
                scriptId = reader.ReadBytes(16);
            } else {
                switch (header.FileVersion) {
                    case < UnitySerializedFileVersion.NewClassId when classId < (int) UnityClassId.Object:
                    case >= UnitySerializedFileVersion.NewClassId when classId == (int) UnityClassId.MonoBehaviour:
                        scriptId = reader.ReadBytes(16);
                        break;
                }
            }

            hash = reader.ReadBytes(16);
        }

        var typeTree = default(UnityTypeTree);
        var className = string.Empty;
        var nameSpace = string.Empty;
        var assemblyName = string.Empty;
        var dependencies = Array.Empty<int>();
        if (header.TypeTreeEnabled) {
            typeTree = UnityTypeTree.FromReader(reader, header, options);

            if (header.FileVersion >= UnitySerializedFileVersion.TypeDependencies) {
                if (isRef) {
                    className = reader.ReadNullString();
                    nameSpace = reader.ReadNullString();
                    assemblyName = reader.ReadNullString();
                } else {
                    dependencies = reader.ReadArray<int>(reader.ReadInt32()).ToArray();
                }
            }
        }

        return new UnitySerializedType(classIdEx, isStrippedType, typeIndex, typeTree, className, nameSpace, assemblyName, dependencies) { Hash = hash, ScriptId = scriptId };
    }

    public static UnitySerializedType[] ArrayFromReader(BiEndianBinaryReader reader, UnitySerializedFile header, SnuggleOptions options, bool isRef = false) {
        var count = reader.ReadInt32();
        var entries = new UnitySerializedType[count];
        for (var i = 0; i < count; ++i) {
            entries[i] = FromReader(reader, header, options, isRef);
        }

        return entries;
    }
}