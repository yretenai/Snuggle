using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Equilibrium.IO;
using Equilibrium.Meta.Interfaces;
using Equilibrium.Meta.Options;
using JetBrains.Annotations;

namespace Equilibrium.Models.Bundle {
    [PublicAPI]
    public record UnityRaw(
        uint Checksum,
        long MinimumStreamedBytes,
        long Size,
        int MinimumBlocks,
        long TotalSize,
        long BlockSize) : IUnityContainer {
        public byte[] Hash { get; set; } = Array.Empty<byte>();

        public UnityBundleBlockInfo[] BlockInfos { get; set; } = Array.Empty<UnityBundleBlockInfo>();
        public UnityBundleBlock[] Blocks { get; set; } = Array.Empty<UnityBundleBlock>();
        public long Length => TotalSize;

        public Stream OpenFile(UnityBundleBlock? block, EquilibriumOptions options, BiEndianBinaryReader? reader = null, Stream? stream = null) {
            if (block == null) {
                return Stream.Null;
            }

            if (stream != null) {
                return new OffsetStream(stream, block.Offset, block.Size, true);
            }

            if (reader == null) {
                throw new NotSupportedException();
            }

            var streamOffset = 0L;
            var cur = -1L;
            stream = new MemoryStream();
            reader.BaseStream.Seek(Size, SeekOrigin.Begin);
            foreach (var (size, compressedSize, unityBundleBlockFlags) in BlockInfos) {
                if (streamOffset + size < block.Offset) {
                    reader.BaseStream.Seek(compressedSize, SeekOrigin.Current);
                    continue;
                }

                if (streamOffset + size > block.Size + block.Offset &&
                    cur > -1) {
                    break;
                }

                if (cur == -1) {
                    cur = block.Offset - streamOffset;
                }

                var compressionType = (UnityCompressionType) (unityBundleBlockFlags & UnityBundleBlockInfoFlags.CompressionMask);
                switch (compressionType) {
                    case UnityCompressionType.None:
                        stream.Write(reader.ReadBytes(compressedSize));
                        break;
                    case UnityCompressionType.LZMA:
                        Utils.DecodeLZMA(reader.BaseStream, compressedSize, size, stream);
                        break;
                    case UnityCompressionType.LZ4:
                    case UnityCompressionType.LZ4HC:
                        Utils.DecompressLZ4(reader.BaseStream, compressedSize, size, stream);
                        break;
                    default:
                        throw new InvalidOperationException();
                }

                streamOffset += size;
            }

            stream.Seek((int) cur, SeekOrigin.Begin);
            return new OffsetStream(stream, cur, block.Size);
        }

        public void ToWriter(BiEndianBinaryWriter writer, UnityBundle header, EquilibriumOptions options, UnityBundleBlock[] blocks, Stream blockStream, BundleSerializationOptions serializationOptions) {
            throw new NotImplementedException();
        }

        public static UnityRaw FromReader(BiEndianBinaryReader reader, UnityBundle header, EquilibriumOptions options) {
            var hash = Array.Empty<byte>();
            var checksum = 0u;
            if (header.FormatVersion >= 4) {
                hash = reader.ReadBytes(16);
                checksum = reader.ReadUInt32();
            }

            var minimumBytes = reader.ReadUInt32();
            var size = reader.ReadUInt32();
            var minimumBlockInfos = reader.ReadInt32();
            var blockInfoCount = reader.ReadInt32();
            var blockInfos = UnityBundleBlockInfo.ArrayFromReader(reader, header, blockInfoCount, options);
            Debug.Assert(blockInfoCount == 1, "blockInfoCount == 1"); // I haven't seen files that have more than 1.
            var totalSize = header.FormatVersion >= 2 ? reader.ReadUInt32() : size + blockInfos.Sum(x => x.Size);

            var blockSize = 0L;
            if (header.FormatVersion >= 3) {
                blockSize = reader.ReadUInt32();
            }

            var unityRaw = new UnityRaw(checksum, minimumBytes, size, minimumBlockInfos, totalSize, blockSize) { Hash = hash, BlockInfos = blockInfos };
            var testBlock = new UnityBundleBlock(0, header.FormatVersion >= 3 ? blockSize : blockInfos[0].Size, 0, string.Empty);
            using var blockReader = new BiEndianBinaryReader(unityRaw.OpenFile(testBlock, options, reader), true);
            var blockCount = blockReader.ReadInt32();
            unityRaw.Blocks = UnityBundleBlock.ArrayFromReader(blockReader, header, blockCount, options);
            return unityRaw;
        }
    }
}
