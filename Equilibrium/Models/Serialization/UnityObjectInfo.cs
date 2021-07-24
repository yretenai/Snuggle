﻿using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Equilibrium.IO;
using JetBrains.Annotations;

namespace Equilibrium.Models.Serialization {
    [PublicAPI]
    public record UnityObjectInfo(
        ulong PathId,
        long Offset,
        long Size,
        int TypeId,
        ClassId ClassId,
        int TypeIndex,
        bool IsDestroyed, // probably a flag. 
        short ScriptTypeIndex,
        bool IsStripped) {
        public static UnityObjectInfo FromReader(BiEndianBinaryReader reader, UnitySerializedFile header, ImmutableArray<UnitySerializedType> types) {
            var pathId = header.BigIdEnabled ? reader.ReadUInt64() : reader.ReadUInt32();
            var offset = header.Version >= UnitySerializedFileVersion.LargeFiles ? reader.ReadInt64() : reader.ReadUInt32();
            var size = reader.ReadUInt32();
            var typeId = reader.ReadInt32();
            ClassId classId;
            var typeIndex = typeId;
            if (header.Version < UnitySerializedFileVersion.NewClassId) {
                classId = (ClassId) reader.ReadUInt16();
                typeIndex = types.Select((x, i) => (i, x)).First(x => x.x.ClassId == classId).i;
            } else {
                classId = types[typeId].ClassId;
            }

            var isDestroyed = 0;
            if (header.Version < UnitySerializedFileVersion.ObjectDestroyedRemoved) {
                isDestroyed = reader.ReadUInt16();
                Debug.Assert(isDestroyed is 0 or 1, "isDestroyed is 0 or 1");
            }

            var scriptTypeIndex = header.Version is >= UnitySerializedFileVersion.ScriptTypeIndex and < UnitySerializedFileVersion.NewTypeData ? reader.ReadInt16() : types[typeIndex].ScriptTypeIndex;

            var stripped = false;
            if (header.Version is UnitySerializedFileVersion.StrippedObject or UnitySerializedFileVersion.NewClassId) {
                stripped = reader.ReadBoolean();
            }

            return new UnityObjectInfo(pathId, offset, size, typeId, classId, typeIndex, isDestroyed == 1, scriptTypeIndex, stripped);
        }

        public static ICollection<UnityObjectInfo> ArrayFromReader(BiEndianBinaryReader reader, ref UnitySerializedFile header, ImmutableArray<UnitySerializedType> types) {
            if (header.Version is >= UnitySerializedFileVersion.BigId and < UnitySerializedFileVersion.BigIdAlwaysEnabled) {
                var value = reader.ReadInt32();
                Debug.Assert(value is 0 or 1, "value is 0 or 1"); // i'm not sure if this is a flag or not. booleans are not aligned, so why is this one?
                if (value == 1) {
                    header = header with { BigIdEnabled = true };
                }
            }

            var count = reader.ReadInt32();
            var entries = new List<UnityObjectInfo>(count);
            for (var i = 0; i < count; ++i) {
                entries.Add(FromReader(reader, header, types));
            }

            return entries;
        }
    }
}
