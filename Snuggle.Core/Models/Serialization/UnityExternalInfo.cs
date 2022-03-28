using System;
using Snuggle.Core.IO;
using Snuggle.Core.Options;

namespace Snuggle.Core.Models.Serialization;

public record UnityExternalInfo(string Path, Guid Guid, int Type, string AssetPath) {
    public string Name { get; } = System.IO.Path.GetFileName(AssetPath);
    public bool IsArchiveReference { get; } = AssetPath.StartsWith("archive:/", StringComparison.InvariantCultureIgnoreCase);

    public static UnityExternalInfo FromReader(BiEndianBinaryReader reader, UnitySerializedFile header, SnuggleCoreOptions options) {
        var path = string.Empty;
        if (header.FileVersion >= UnitySerializedFileVersion.ExternalExtraPath) {
            path = reader.ReadNullString();
        }

        var guid = Guid.Empty;
        var type = 0;
        if (header.FileVersion >= UnitySerializedFileVersion.ExternalGuid) {
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

    public static UnityExternalInfo[] ArrayFromReader(BiEndianBinaryReader reader, UnitySerializedFile header, SnuggleCoreOptions options) {
        var count = reader.ReadInt32();
        var array = new UnityExternalInfo[count];
        for (var i = 0; i < count; ++i) {
            array[i] = FromReader(reader, header, options);
        }

        return array;
    }
}
