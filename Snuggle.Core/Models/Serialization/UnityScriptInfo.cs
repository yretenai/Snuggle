using System;
using Snuggle.Core.IO;
using Snuggle.Core.Options;

namespace Snuggle.Core.Models.Serialization;

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
    
    public static void ArrayToWriter(BiEndianBinaryWriter writer, UnityScriptInfo[] infos, UnitySerializedFile header, SnuggleCoreOptions options, AssetSerializationOptions serializationOptions) {
        writer.Write(infos.Length);
        foreach (var info in infos) {
            info.ToWriter(writer, header, options, serializationOptions);
        }
    }

    public void ToWriter(BiEndianBinaryWriter writer, UnitySerializedFile header, SnuggleCoreOptions options, AssetSerializationOptions serializationOptions) {
        if (serializationOptions.TargetFileVersion >= UnitySerializedFileVersion.BigIdAlwaysEnabled) {
            writer.Align();
        }

        writer.Write(Index);
        writer.Write(PathId);
    }
}
