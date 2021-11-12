using System;
using System.IO;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using K4os.Compression.LZ4;
using Snuggle.Core.IO;
using Snuggle.Core.Options;

namespace Snuggle.Core.Models.Bundle;

[PublicAPI]
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

    public static void ToWriter(BiEndianBinaryWriter writer, UnityBundle header, SnuggleCoreOptions options, BundleSerializationOptions serializationOptions, Stream blockDataStream, Stream blockStream) {
        var (blockSize, _, blockCompressionType) = serializationOptions;
        var actualBlockSize = (int) Math.Min(blockStream.Length - blockStream.Position, blockSize);
        writer.Write(actualBlockSize);
        using var chunk = new MemoryStream();
        switch (blockCompressionType) {
            case UnityCompressionType.None: {
                var pooled = Utils.BytePool.Rent(0x8000000);
                try {
                    while (actualBlockSize > 0) {
                        var amount = blockStream.Read(pooled);
                        actualBlockSize -= amount;
                        chunk.Write(pooled.AsSpan()[..amount]);
                    }
                } finally {
                    Utils.BytePool.Return(pooled);
                }

                break;
            }
            case UnityCompressionType.LZMA: {
                Utils.EncodeLZMA(chunk, blockStream, actualBlockSize);
                break;
            }
            case UnityCompressionType.LZ4: // this is deprecated.
            case UnityCompressionType.LZ4HC: {
                Utils.CompressLZ4(blockStream, chunk, LZ4Level.L12_MAX, actualBlockSize);
                break;
            }
            default:
                throw new NotSupportedException($"Unity Compression type {blockCompressionType:G} is not supported");
        }

        writer.Write((uint) chunk.Length);
        writer.Write((ushort) serializationOptions.BlockCompressionType);
        chunk.Seek(0, SeekOrigin.Begin);
        chunk.CopyTo(blockDataStream);
    }
}
