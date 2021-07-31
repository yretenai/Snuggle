using System.IO;
using Equilibrium.IO;
using Equilibrium.Options;
using JetBrains.Annotations;

namespace Equilibrium.Models.Serialization {
    [PublicAPI]
    public record UnitySerializedFile (
        int HeaderSize,
        long Size,
        UnitySerializedFileVersion Version,
        long Offset,
        bool IsBigEndian,
        ulong LargeAddressableFlags,
        string UnityVersion,
        UnityPlatform Platform,
        bool TypeTreeEnabled,
        bool BigIdEnabled) {
        public static UnitySerializedFile FromReader(BiEndianBinaryReader reader, EquilibriumOptions options) {
            var headerSize = reader.ReadInt32();
            long size = reader.ReadInt32();
            var version = (UnitySerializedFileVersion) reader.ReadUInt32();
            long offset = reader.ReadInt32();
            var laf = 0ul;

            if (version < UnitySerializedFileVersion.HeaderContentAtFront) {
                reader.BaseStream.Seek(size - headerSize, SeekOrigin.Begin);
            }

            var isBigEndian = reader.ReadBoolean();
            reader.Align();

            if (version >= UnitySerializedFileVersion.LargeFiles) {
                headerSize = reader.ReadInt32();
                size = reader.ReadInt64();
                offset = reader.ReadInt64();
                laf = reader.ReadUInt64();
            }

            reader.IsBigEndian = isBigEndian;

            var unityVersion = string.Empty;
            if (version >= UnitySerializedFileVersion.UnityVersion) {
                unityVersion = reader.ReadNullString();
            }

            var targetPlatform = UnityPlatform.Unknown;
            if (version >= UnitySerializedFileVersion.TargetPlatform) {
                targetPlatform = (UnityPlatform) reader.ReadInt32();
            }

            var typeTreeEnabled = true;
            if (version >= UnitySerializedFileVersion.TypeTreeEnabledSwitch) {
                typeTreeEnabled = reader.ReadBoolean();
            }

            return new UnitySerializedFile(headerSize, size, version, offset, isBigEndian, laf, unityVersion, targetPlatform, typeTreeEnabled, version >= UnitySerializedFileVersion.BigIdAlwaysEnabled);
        }
    }
}
