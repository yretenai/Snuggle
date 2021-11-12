using System;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using Snuggle.Core.IO;
using Snuggle.Core.Options;

namespace Snuggle.Core.Models.Serialization;

[PublicAPI]
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
    public static UnityObjectInfo FromReader(BiEndianBinaryReader reader, UnitySerializedFile header, UnitySerializedType[] types, SnuggleOptions options) {
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

        return new UnityObjectInfo(pathId, offset, size, typeId, classIdEx, typeIndex, isDestroyed == 1, scriptTypeIndex, stripped);
    }

    public static UnityObjectInfo[] ArrayFromReader(BiEndianBinaryReader reader, ref UnitySerializedFile header, UnitySerializedType[] types, SnuggleOptions options) {
        if (header.FileVersion is >= UnitySerializedFileVersion.BigId and < UnitySerializedFileVersion.BigIdAlwaysEnabled) {
            var value = reader.ReadInt32();
            if (value != 1) {
                throw new NotSupportedException("Legacy PathIds are not supported");
            }
        }

        var count = reader.ReadInt32();
        var entries = new UnityObjectInfo[count];
        for (var i = 0; i < count; ++i) {
            entries[i] = FromReader(reader, header, types, options);
        }

        return entries;
    }
}
