using System;
using Snuggle.Core.IO;
using Snuggle.Core.Options;

namespace Snuggle.Core.Models.Bundle;

public record UnityBundleBlock(long Offset, long Size, uint Flags, string Path) {
    public static UnityBundleBlock FromReader(BiEndianBinaryReader reader, int fsFlags, SnuggleCoreOptions options) {
        var offset = reader.ReadInt64();
        var size = reader.ReadInt64();
        var flags = reader.ReadUInt32();
        var path = reader.ReadNullString();

        return new UnityBundleBlock(offset, size, flags, path);
    }

    public static UnityBundleBlock FromReaderRaw(BiEndianBinaryReader reader, int fsFlags, SnuggleCoreOptions options) {
        var path = reader.ReadNullString();
        var offset = reader.ReadUInt32();
        var size = reader.ReadUInt32();
        return new UnityBundleBlock(offset, size, 4, path);
    }

    public static UnityBundleBlock[] ArrayFromReader(BiEndianBinaryReader reader, UnityBundle header, int fsFlags, int count, SnuggleCoreOptions options) {
        switch (header.Format) {
            case UnityFormat.FS: {
                var container = new UnityBundleBlock[count];
                for (var i = 0; i < count; ++i) {
                    container[i] = FromReader(reader, fsFlags, options);
                }

                return container;
            }
            case UnityFormat.Raw:
            case UnityFormat.Web: {
                var container = new UnityBundleBlock[count];
                for (var i = 0; i < count; ++i) {
                    container[i] = FromReaderRaw(reader, fsFlags, options);
                }

                return container;
            }
            case UnityFormat.Archive:
                throw new NotImplementedException();
            default:
                throw new NotSupportedException($"Unity Bundle format {header.Signature} is not supported");
        }
    }

    public static void ArrayToWriter(BiEndianBinaryWriter writer, UnityBundleBlock[] blocks, UnityBundle header, SnuggleCoreOptions options, BundleSerializationOptions serializationOptions) {
        writer.Write(blocks.Length);

        var offset = 0L;
        foreach (var block in blocks) {
            block.ToWriter(writer, header, offset);
            offset += block.Size; // Alignment? ModCheck
        }
    }

    private void ToWriter(BiEndianBinaryWriter writer, UnityBundle header, long offset) {
        if (header.Format == UnityFormat.FS) {
            writer.Write(offset);
            writer.Write(Size);
            writer.Write((uint) Flags);
            writer.WriteNullString(Path);
        } else {
            writer.WriteNullString(Path);
            writer.Write(offset);
            writer.Write(Size);
        }
    }
}
