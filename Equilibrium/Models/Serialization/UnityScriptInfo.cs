using System.Collections.Generic;
using System.Collections.Immutable;
using Equilibrium.IO;
using JetBrains.Annotations;

namespace Equilibrium.Models.Serialization {
    [PublicAPI]
    public record UnityScriptInfo(
        int Index,
        ulong PathId) {
        public static UnityScriptInfo FromReader(BiEndianBinaryReader reader, UnitySerializedFile header) {
            if (header.Version >= UnitySerializedFileVersion.BigIdAlwaysEnabled) {
                reader.Align();
            }

            var index = reader.ReadInt32();
            var identifier = header.BigIdEnabled ? reader.ReadUInt64() : reader.ReadUInt32();
            return new UnityScriptInfo(index, identifier);
        }

        public static ICollection<UnityScriptInfo> ArrayFromReader(BiEndianBinaryReader reader, UnitySerializedFile header) {
            if (header.Version <= UnitySerializedFileVersion.ScriptTypeIndex) {
                return ImmutableList<UnityScriptInfo>.Empty;
            }

            var count = reader.ReadInt32();
            var array = new List<UnityScriptInfo>(count);
            for (var i = 0; i < count; ++i) {
                array.Add(FromReader(reader, header));
            }

            return array;
        }
    }
}
