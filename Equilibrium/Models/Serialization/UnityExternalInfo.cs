using System;
using Equilibrium.IO;
using JetBrains.Annotations;

namespace Equilibrium.Models.Serialization {
    [PublicAPI]
    public record UnityExternalInfo(
        string Path,
        Guid Guid,
        int Type,
        string AssetPath) {
        public static UnityExternalInfo FromReader(BiEndianBinaryReader reader, UnitySerializedFile header) {
            var path = string.Empty;
            if (header.Version >= UnitySerializedFileVersion.ExternalExtraPath) {
                path = reader.ReadNullString();
            }

            var guid = Guid.Empty;
            var type = 0;
            if (header.Version >= UnitySerializedFileVersion.ExternalGuid) {
                guid = new Guid(reader.ReadBytes(16));
                type = reader.ReadInt32();
            }

            var assetPath = reader.ReadNullString();
            if (assetPath.StartsWith("resources/")) {
                assetPath = "R" + assetPath[1..];
            } else if (assetPath.StartsWith("library/", StringComparison.InvariantCultureIgnoreCase)) {
                assetPath = "Resources/" + assetPath[8..];
            }

            return new UnityExternalInfo(path, guid, type, assetPath);
        }

        public static UnityExternalInfo[] ArrayFromReader(BiEndianBinaryReader reader, UnitySerializedFile header) {
            var count = reader.ReadInt32();
            var array = new UnityExternalInfo[count];
            for (var i = 0; i < count; ++i) {
                array[i] = FromReader(reader, header);
            }

            return array;
        }
    }
}
