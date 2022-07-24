using System;
using Snuggle.Core.IO;
using Snuggle.Core.Options;

namespace Snuggle.Core.Models.Serialization;

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

    public static UnitySerializedType FromReader(BiEndianBinaryReader reader, UnitySerializedFile header, SnuggleCoreOptions options, bool isRef = false) {
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

        return new UnitySerializedType(
            classIdEx,
            isStrippedType,
            typeIndex,
            typeTree,
            className,
            nameSpace,
            assemblyName,
            dependencies) { Hash = hash, ScriptId = scriptId };
    }

    public static UnitySerializedType[] ArrayFromReader(BiEndianBinaryReader reader, UnitySerializedFile header, SnuggleCoreOptions options, bool isRef = false) {
        var count = reader.ReadInt32();
        var entries = new UnitySerializedType[count];
        for (var i = 0; i < count; ++i) {
            entries[i] = FromReader(reader, header, options, isRef);
        }

        return entries;
    }
    
    public static void ArrayToWriter(BiEndianBinaryWriter writer, UnitySerializedType[] types, UnitySerializedFile header, SnuggleCoreOptions options, AssetSerializationOptions serializationOptions, bool isRef = false) {
        writer.Write(types.Length);
        foreach (var type in types) {
            type.ToWriter(writer, header, options, serializationOptions, isRef);
        }
    }

    public void ToWriter(BiEndianBinaryWriter writer, UnitySerializedFile header, SnuggleCoreOptions options, AssetSerializationOptions serializationOptions, bool isRef = false) {
        var classId = (int)ClassId;
        writer.Write(classId);
        if (serializationOptions.TargetFileVersion >= UnitySerializedFileVersion.StrippedType) {
            writer.Write(IsStrippedType);
        }

        if (serializationOptions.TargetFileVersion >= UnitySerializedFileVersion.NewTypeData) {
            writer.Write(ScriptTypeIndex);
        }

        if (serializationOptions.TargetFileVersion >= UnitySerializedFileVersion.TypeTreeHash) {
            if (isRef && ScriptTypeIndex >= 0) {
                writer.Write(ScriptId);
            } else {
                switch (serializationOptions.TargetFileVersion) {
                    case < UnitySerializedFileVersion.NewClassId when classId < (int) UnityClassId.Object:
                    case >= UnitySerializedFileVersion.NewClassId when classId == (int) UnityClassId.MonoBehaviour:
                        writer.Write(ScriptId);
                        break;
                }
            }

            writer.Write(Hash);
        }
        
        if (header.TypeTreeEnabled) {
            TypeTree!.ToWriter(writer, header, options, serializationOptions);

            if (header.FileVersion >= UnitySerializedFileVersion.TypeDependencies) {
                if (isRef) {
                    writer.WriteNullString(ClassName);
                    writer.WriteNullString(NameSpace);
                    writer.WriteNullString(AssemblyName);
                } else {
                    writer.WriteArray(Dependencies);
                }
            }
        }
    }
}
