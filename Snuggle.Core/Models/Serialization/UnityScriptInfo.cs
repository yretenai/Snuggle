using System;
using JetBrains.Annotations;
using Snuggle.Core.IO;
using Snuggle.Core.Options;

namespace Snuggle.Core.Models.Serialization;

[PublicAPI]
public record UnityScriptInfo(int Index, long PathId) {
    public static UnityScriptInfo FromReader(BiEndianBinaryReader reader, UnitySerializedFile header, SnuggleCoreOptions options) {
        if (header.FileVersion >= UnitySerializedFileVersion.BigIdAlwaysEnabled) {
            reader.Align();
        }

        var index = reader.ReadInt32();
        var identifier = reader.ReadInt64();
        return new UnityScriptInfo(index, identifier);
    }

    public static UnityScriptInfo[] ArrayFromReader(BiEndianBinaryReader reader, UnitySerializedFile header, SnuggleCoreOptions options) {
        if (header.FileVersion <= UnitySerializedFileVersion.ScriptTypeIndex) {
            return Array.Empty<UnityScriptInfo>();
        }

        var count = reader.ReadInt32();
        var array = new UnityScriptInfo[count];
        for (var i = 0; i < count; ++i) {
            array[i] = FromReader(reader, header, options);
        }

        return array;
    }
}
