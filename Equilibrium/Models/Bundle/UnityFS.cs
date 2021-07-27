using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using DragonLib;
using Equilibrium.IO;
using Equilibrium.Meta;
using JetBrains.Annotations;

namespace Equilibrium.Models.Bundle {
    [PublicAPI]
    public record UnityFS(
        long Size,
        int CompressedBlockInfoSize,
        int BlockInfoSize,
        UnityFSFlags Flags) : IUnityContainer {
        public byte[] Hash { get; set; } = new byte[16];
        public UnityBundleBlockInfo[] BlockInfos { get; set; } = Array.Empty<UnityBundleBlockInfo>();
        public UnityBundleBlock[] Blocks { get; set; } = Array.Empty<UnityBundleBlock>();
        public long Length => Size;

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

                var buffer = reader.ReadBytes(compressedSize);

                var compressionType = (UnityCompressionType) (unityBundleBlockFlags & UnityBundleBlockInfoFlags.CompressionMask);
                switch (compressionType) {
                    case UnityCompressionType.None:
                        stream.Write(buffer);
                        break;
                    case UnityCompressionType.LZMA:
                        Utils.DecodeLZMA(buffer, compressedSize, size, stream);
                        break;
                    case UnityCompressionType.LZ4:
                    case UnityCompressionType.LZ4HC:
                        stream.Write(CompressionEncryption.DecompressLZ4(buffer, size));
                        break;
                    default:
                        throw new InvalidOperationException();
                }

                streamOffset += size;
            }

            stream.Seek((int) cur, SeekOrigin.Begin);
            return new OffsetStream(stream, cur, block.Size);
        }

        public void ToWriter(BiEndianBinaryWriter writer, UnityBundle header, EquilibriumOptions options, UnityBundleBlock[] blocks, Stream blockStream, EquilibriumSerializationOptions serializationOptions) {
            var start = writer.BaseStream.Position;
            writer.Write(0L);
            writer.Write(0);
            writer.Write(0);
            writer.Write((int) (UnityFSFlags.CombinedData | (UnityFSFlags) serializationOptions.CompressionType));
            if (header.FormatVersion >= 7) {
                writer.Align(16);
            }

            using var blockInfoStream = new MemoryStream();
            using var blockDataStream = new MemoryStream();
            using var blockInfoWriter = new BiEndianBinaryWriter(blockInfoStream, true);
            blockInfoWriter.Write(Hash);
            var (blockSize, unityCompressionType) = serializationOptions;
            var blockLength = blocks.Sum(x => x.Size);
            var blockInfoCount = (int) Math.Ceiling((double) blockLength / blockSize);
            var blockInfoStart = blockInfoStream.Position;
            var blockStart = blockInfoStart + 4 + blockInfoCount * 10;
            blockInfoStream.Seek(blockStart, SeekOrigin.Begin);
            UnityBundleBlock.ArrayToWriter(blockInfoWriter, blocks, header, options, serializationOptions);
            blockInfoStream.Seek(blockInfoStart, SeekOrigin.Begin);
            blockInfoWriter.Write(blockInfoCount);
            for(var i = 0; i < blockInfoCount; ++i) {
                UnityBundleBlockInfo.ToWriter(blockInfoWriter, header, options, serializationOptions, blockDataStream, blockStream);
            }

            var blockInfoSize = (int) blockInfoStream.Length;
            int compressedBlockInfoSize;
            if (unityCompressionType == UnityCompressionType.None) {
                compressedBlockInfoSize = blockInfoSize;
                blockInfoStream.Seek(0, SeekOrigin.Begin);
                blockInfoStream.CopyTo(writer.BaseStream);
            } else {
                throw new NotImplementedException();
            }
            blockDataStream.Seek(0, SeekOrigin.Begin);
            blockDataStream.CopyTo(writer.BaseStream);

            writer.BaseStream.Seek(start, SeekOrigin.Begin);
            writer.Write(writer.BaseStream.Length - start);
            writer.Write(blockInfoSize);
            writer.Write(compressedBlockInfoSize);
        }

        public static UnityFS FromReader(BiEndianBinaryReader reader, UnityBundle header, EquilibriumOptions options) {
            var size = reader.ReadInt64();
            var compressedBlockSize = reader.ReadInt32();
            var blockSize = reader.ReadInt32();
            var flags = (UnityFSFlags) reader.ReadUInt32();
            if (header.FormatVersion >= 7) {
                reader.Align(16);
            }

            var fs = new UnityFS(size, compressedBlockSize, blockSize, flags);
            var blocksBuffer = new byte[fs.CompressedBlockInfoSize];
            if (fs.Flags.HasFlag(UnityFSFlags.BlocksInfoAtEnd)) {
                var tmp = reader.BaseStream.Position;
                reader.BaseStream.Seek(fs.CompressedBlockInfoSize, SeekOrigin.End);
                reader.Read(blocksBuffer);
                reader.BaseStream.Seek(tmp, SeekOrigin.Begin);
            } else {
                reader.Read(blocksBuffer);
            }

            var compressionType = (UnityCompressionType) (fs.Flags & UnityFSFlags.CompressionRange);
            using var blocksReader = compressionType switch {
                UnityCompressionType.None => BiEndianBinaryReader.FromArray(blocksBuffer, true),
                UnityCompressionType.LZMA => new BiEndianBinaryReader(Utils.DecodeLZMA(blocksBuffer, fs.CompressedBlockInfoSize, fs.BlockInfoSize), true),
                UnityCompressionType.LZ4 => BiEndianBinaryReader.FromArray(CompressionEncryption.DecompressLZ4(blocksBuffer, fs.BlockInfoSize).ToArray(), true),
                UnityCompressionType.LZ4HC => BiEndianBinaryReader.FromArray(CompressionEncryption.DecompressLZ4(blocksBuffer, fs.BlockInfoSize).ToArray(), true),
                _ => throw new InvalidOperationException(),
            };
            fs.Hash = blocksReader.ReadBytes(16);
            var infoCount = blocksReader.ReadInt32();
            fs.BlockInfos = UnityBundleBlockInfo.ArrayFromReader(blocksReader, header, infoCount, options);
            var blockCount = blocksReader.ReadInt32();
            fs.Blocks = UnityBundleBlock.ArrayFromReader(blocksReader, header, blockCount, options);

            return fs;
        }
    }
}
