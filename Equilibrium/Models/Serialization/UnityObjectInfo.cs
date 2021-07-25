using System.Diagnostics;
using System.Linq;
using Equilibrium.IO;
using JetBrains.Annotations;

namespace Equilibrium.Models.Serialization {
    [PublicAPI]
    public record UnityObjectInfo(
        long PathId,
        long Offset,
        long Size,
        int TypeId,
        ClassId ClassId,
        int TypeIndex,
        bool IsDestroyed, // probably a flag. 
        short ScriptTypeIndex,
        bool IsStripped) {
        public static UnityObjectInfo FromReader(BiEndianBinaryReader reader, UnitySerializedFile header, UnitySerializedType[] types) {
            if (header.Version >= UnitySerializedFileVersion.BigIdAlwaysEnabled) {
                reader.Align();
            }

            var pathId = header.BigIdEnabled ? reader.ReadInt64() : reader.ReadInt32();
            var offset = header.Version >= UnitySerializedFileVersion.LargeFiles ? reader.ReadInt64() : reader.ReadInt32();
            var size = reader.ReadUInt32();
            var typeId = reader.ReadInt32();
            ClassId classId;
            var typeIndex = typeId;
            if (header.Version < UnitySerializedFileVersion.NewClassId) {
                classId = (ClassId) reader.ReadUInt16();
                typeIndex = types.Select((x, i) => (i, x)).First(x => x.x.ClassId == (ClassId) typeId).i;
            } else {
                classId = types.ElementAt(typeId).ClassId;
            }

            var isDestroyed = 0;
            if (header.Version < UnitySerializedFileVersion.ObjectDestroyedRemoved) {
                isDestroyed = reader.ReadUInt16();
                Debug.Assert(isDestroyed is 0 or 1, "isDestroyed is 0 or 1");
            }

            var scriptTypeIndex = header.Version is >= UnitySerializedFileVersion.ScriptTypeIndex and < UnitySerializedFileVersion.NewTypeData ? reader.ReadInt16() : types.ElementAt(typeId).ScriptTypeIndex;

            var stripped = false;
            if (header.Version is UnitySerializedFileVersion.StrippedObject or UnitySerializedFileVersion.NewClassId) {
                stripped = reader.ReadBoolean();
            }

            return new UnityObjectInfo(pathId, offset, size, typeId, classId, typeIndex, isDestroyed == 1, scriptTypeIndex, stripped);
        }

        public static UnityObjectInfo[] ArrayFromReader(BiEndianBinaryReader reader, ref UnitySerializedFile header, UnitySerializedType[] types) {
            if (header.Version is >= UnitySerializedFileVersion.BigId and < UnitySerializedFileVersion.BigIdAlwaysEnabled) {
                var value = reader.ReadInt32();
                Debug.Assert(value is 0 or 1, "value is 0 or 1"); // i'm not sure if this is a flag or not. booleans are not aligned, so why is this one?
                if (value == 1) {
                    header = header with { BigIdEnabled = true };
                }
            }

            var count = reader.ReadInt32();
            var entries = new UnityObjectInfo[count];
            for (var i = 0; i < count; ++i) {
                entries[i] = FromReader(reader, header, types);
            }

            return entries;
        }
    }
}
