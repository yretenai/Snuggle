using System;
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using Equilibrium.IO;
using Equilibrium.Options;
using JetBrains.Annotations;
using K4os.Compression.LZ4;

namespace Equilibrium.Models.Bundle {
    [PublicAPI, StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record UnityBundleBlockInfo(
        int Size,
        int CompressedSize,
        UnityBundleBlockInfoFlags Flags) {
        public static UnityBundleBlockInfo FromReader(BiEndianBinaryReader reader, UnityBundle header, EquilibriumOptions options) {
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

        public static UnityBundleBlockInfo[] ArrayFromReader(BiEndianBinaryReader reader, UnityBundle header, int count, EquilibriumOptions options) {
            var container = new UnityBundleBlockInfo[count];
            switch (header.Format) {
                case UnityFormat.FS:
                case UnityFormat.Raw:
                case UnityFormat.Web: {
                    for (var i = 0; i < count; ++i) {
                        container[i] = FromReader(reader, header, options);
                    }
                }
                    break;
                case UnityFormat.Archive:
                    throw new NotImplementedException();
                default:
                    throw new InvalidOperationException();
            }

            return container;
        }

        public static void ToWriter(BiEndianBinaryWriter writer, UnityBundle header, EquilibriumOptions options, BundleSerializationOptions serializationOptions, Stream blockDataStream, Stream blockStream) {
            var (blockSize, _, blockCompressionType) = serializationOptions;
            var actualBlockSize = (int) Math.Min(blockStream.Length - blockStream.Position, blockSize);
            writer.Write(actualBlockSize);
            using var chunk = new MemoryStream();
            switch (blockCompressionType) {
                case UnityCompressionType.None: {
                    var pooled = ArrayPool<byte>.Shared.Rent(0x8000000);
                    try {
                        while (actualBlockSize > 0) {
                            var amount = blockStream.Read(pooled);
                            actualBlockSize -= amount;
                            chunk.Write(pooled.AsSpan()[..amount]);
                        }
                    } finally {
                        ArrayPool<byte>.Shared.Return(pooled);
                    }

                    break;
                }
                case UnityCompressionType.LZMA: {
                    Utils.EncodeLZMA(chunk, blockStream, actualBlockSize);
                    break;
                }
                case UnityCompressionType.LZ4:
                case UnityCompressionType.LZ4HC: {
                    Utils.CompressLZ4(blockStream, chunk, blockCompressionType == UnityCompressionType.LZ4HC ? LZ4Level.L12_MAX : LZ4Level.L00_FAST, actualBlockSize);
                    break;
                }
                default:
                    throw new NotSupportedException();
            }

            writer.Write((uint) chunk.Length);
            writer.Write((ushort) serializationOptions.BlockCompressionType);
            chunk.Seek(0, SeekOrigin.Begin);
            chunk.CopyTo(blockDataStream);
        }
    }
}
