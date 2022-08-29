using System;
using System.IO;
using System.Runtime.InteropServices;
using K4os.Compression.LZ4;
using Snuggle.Core.IO;
using Snuggle.Core.Options;

namespace Snuggle.Core.Models.Bundle;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record UnityBundleBlockInfo(int Size, int CompressedSize, UnityBundleBlockInfoFlags Flags) {
    public static UnityBundleBlockInfo FromReader(BiEndianBinaryReader reader, UnityBundle header, int fsFlags, SnuggleCoreOptions options) {
        var size = reader.ReadInt32();
        var compressedSize = reader.ReadInt32();
        var flags = header.Format switch {
            UnityFormat.FS => (UnityBundleBlockInfoFlags) reader.ReadInt16(),
            UnityFormat.Raw => (UnityBundleBlockInfoFlags) 0,
            UnityFormat.Web => (UnityBundleBlockInfoFlags) 1,
            _ => (UnityBundleBlockInfoFlags) 0,
        };
        return new UnityBundleBlockInfo(size, compressedSize, flags);
    }

    public static UnityBundleBlockInfo[] ArrayFromReader(BiEndianBinaryReader reader, UnityBundle header, int fsFlags, int count, SnuggleCoreOptions options) {
        var container = new UnityBundleBlockInfo[count];
        switch (header.Format) {
            case UnityFormat.FS:
            case UnityFormat.Raw:
            case UnityFormat.Web: {
                for (var i = 0; i < count; ++i) {
                    container[i] = FromReader(reader, header, fsFlags, options);
                }
            }
                break;
            case UnityFormat.Archive:
                throw new NotImplementedException();
            default:
                throw new NotSupportedException($"Unity Bundle format {header.Signature} is not supported");
        }

        return container;
    }

    public static void ToWriter(BiEndianBinaryWriter writer, UnityBundle header, BundleSerializationOptions serializationOptions, Stream blockDataStream, Stream blockData) {
        writer.Write(blockData.Length);
        blockData.Seek(0, SeekOrigin.Begin);
        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (serializationOptions.BlockCompressionType) {
            case UnityCompressionType.None: {
                blockData.CopyTo(blockDataStream);
                break;
            }
            case UnityCompressionType.LZMA: {
                Utils.EncodeLZMA(blockDataStream, blockData, blockData.Length);
                break;
            }
            default:
                throw new NotSupportedException($"Unity Compression type {serializationOptions.BlockCompressionType:G} is not supported as single chunk");
        }

        writer.Write(blockDataStream.Length);
        if (header.Format == UnityFormat.FS) {
            writer.Write((ushort) UnityCompressionType.None);
        }
    }

    public static void ToWriterChunked(BiEndianBinaryWriter writer, UnityBundle header, SnuggleCoreOptions options, BundleSerializationOptions serializationOptions, Stream blockDataStream, Span<byte> blockData) {
        writer.Write(blockData.Length);
        using var chunk = new MemoryStream();
        switch (serializationOptions.BlockCompressionType) {
            case UnityCompressionType.LZ4: {
                Utils.CompressLZ4(blockData, chunk, LZ4Level.L00_FAST);
                break;
            }
            case UnityCompressionType.LZ4HC: {
                Utils.CompressLZ4(blockData, chunk, LZ4Level.L12_MAX);
                break;
            }
            default:
                throw new NotSupportedException($"Unity Compression type {serializationOptions.BlockCompressionType:G} is not supported as chunked");
        }

        writer.Write((uint) chunk.Length);
        if (header.Format == UnityFormat.FS) {
            writer.Write((ushort) serializationOptions.BlockCompressionType);
        }

        chunk.Seek(0, SeekOrigin.Begin);
        chunk.CopyTo(blockDataStream);
    }
}
