using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Snuggle.Core.Implementations;
using Snuggle.Core.IO;
using Snuggle.Core.Options;

namespace Snuggle.Core.Models.Serialization;

public record UnityObjectInfo(
    long PathId,
    long Offset,
    long Size,
    int TypeId,
    object ClassId,
    int TypeIndex,
    bool IsDestroyed, // probably a flag. 
    short ScriptTypeIndex,
    bool IsStripped) {
    public static UnityObjectInfo FromReader(BiEndianBinaryReader reader, UnitySerializedFile header, UnitySerializedType[] types, SnuggleCoreOptions options) {
        if (header.FileVersion >= UnitySerializedFileVersion.BigIdAlwaysEnabled) {
            reader.Align();
        }

        var pathId = reader.ReadInt64();
        var offset = header.FileVersion >= UnitySerializedFileVersion.LargeFiles ? reader.ReadInt64() : reader.ReadInt32();
        var size = reader.ReadUInt32();
        var typeId = reader.ReadInt32();
        int classId;
        var typeIndex = typeId;
        if (header.FileVersion < UnitySerializedFileVersion.NewClassId) {
            classId = reader.ReadUInt16();
            typeIndex = types.Select((x, i) => (i, x)).First(x => (int) x.x.ClassId == typeId).i;
        } else {
            classId = (int) types.ElementAt(typeId).ClassId;
        }

        var classIdEx = ObjectFactory.GetClassIdForGame(options.Game, classId);

        var isDestroyed = 0;
        if (header.FileVersion < UnitySerializedFileVersion.ObjectDestroyedRemoved) {
            isDestroyed = reader.ReadUInt16();
            Debug.Assert(isDestroyed is 0 or 1, "isDestroyed is 0 or 1");
        }

        var scriptTypeIndex = header.FileVersion is >= UnitySerializedFileVersion.ScriptTypeIndex and < UnitySerializedFileVersion.NewTypeData ? reader.ReadInt16() : types.ElementAt(typeId).ScriptTypeIndex;

        var stripped = false;
        if (header.FileVersion is UnitySerializedFileVersion.StrippedObject or UnitySerializedFileVersion.NewClassId) {
            stripped = reader.ReadBoolean();
        }

        return new UnityObjectInfo(
            pathId,
            offset,
            size,
            typeId,
            classIdEx,
            typeIndex,
            isDestroyed == 1,
            scriptTypeIndex,
            stripped);
    }

    public static ImmutableArray<UnityObjectInfo> ArrayFromReader(BiEndianBinaryReader reader, ref UnitySerializedFile header, UnitySerializedType[] types, SnuggleCoreOptions options) {
        if (header.FileVersion is >= UnitySerializedFileVersion.BigId and < UnitySerializedFileVersion.BigIdAlwaysEnabled) {
            var value = reader.ReadInt32();
            if (value != 1) {
                throw new NotSupportedException("Legacy PathIds are not supported");
            }
        }

        var count = reader.ReadInt32();
        var entries = ImmutableArray.CreateBuilder<UnityObjectInfo>(count);
        for (var i = 0; i < count; ++i) {
            entries.Add(FromReader(reader, header, types, options));
        }

        return entries.ToImmutable();
    }
    
    public static void ArrayToWriter(BiEndianBinaryWriter writer, ImmutableArray<UnityObjectInfo> infos, UnitySerializedFile header, SnuggleCoreOptions options, AssetSerializationOptions serializationOptions, bool isRef = false) {
        if (serializationOptions.TargetFileVersion is >= UnitySerializedFileVersion.BigId and < UnitySerializedFileVersion.BigIdAlwaysEnabled) {
            writer.Write(1);
        }

        writer.Write(infos.Length);
        foreach (var info in infos) {
            info.ToWriter(writer, header, options, serializationOptions, isRef);
        }
    }

    public void ToWriter(BiEndianBinaryWriter writer, UnitySerializedFile header, SnuggleCoreOptions options, AssetSerializationOptions serializationOptions, bool isRef = false) {
        if (serializationOptions.TargetFileVersion >= UnitySerializedFileVersion.BigIdAlwaysEnabled) {
            writer.Align();
        }

        writer.Write(PathId);
        if (serializationOptions.TargetFileVersion >= UnitySerializedFileVersion.LargeFiles)
            writer.Write(Offset);
        else
            writer.Write((uint)Offset);
        writer.Write((uint)Size);
        writer.Write(TypeId);
        if (serializationOptions.TargetFileVersion < UnitySerializedFileVersion.NewClassId) {
            writer.Write((ushort)(int)ClassId);
        }

        if (serializationOptions.TargetFileVersion < UnitySerializedFileVersion.ObjectDestroyedRemoved) {
            writer.Write((ushort)(IsDestroyed ? 1 : 0));
        }

        if (serializationOptions.TargetFileVersion is >= UnitySerializedFileVersion.ScriptTypeIndex and < UnitySerializedFileVersion.NewTypeData) {
            writer.Write(ScriptTypeIndex);
        }

        if (serializationOptions.TargetFileVersion is UnitySerializedFileVersion.StrippedObject or UnitySerializedFileVersion.NewClassId) {
            writer.Write(IsStripped);
        }
    }
    
    public SerializedObject? Instance { get; set; }
}
