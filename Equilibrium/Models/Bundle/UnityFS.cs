using System;
using System.IO;
using System.Linq;
using Equilibrium.Interfaces;
using Equilibrium.IO;
using Equilibrium.Options;
using JetBrains.Annotations;
using K4os.Compression.LZ4;

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
            stream = new MemoryStream((int) block.Size) { Position = 0 };
            foreach (var (size, compressedSize, unityBundleBlockFlags) in BlockInfos) {
                if (streamOffset + size < block.Offset) {
                    reader.BaseStream.Seek(compressedSize, SeekOrigin.Current);
                    streamOffset += size;
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
            var start = writer.BaseStream.Position;
            writer.Write(0L);
            writer.Write(0);
            writer.Write(0);
            writer.Write((int) (UnityFSFlags.CombinedData | (UnityFSFlags) serializationOptions.CompressionType));
            if (serializationOptions.TargetVersion >= 7) {
                writer.Align(16);
            }

            using var blockInfoStream = new MemoryStream();
            using var blockDataStream = new MemoryStream();
            using var blockInfoWriter = new BiEndianBinaryWriter(blockInfoStream, true);
            blockInfoWriter.Write(Hash);
            var (blockSize, unityCompressionType, _, _, _) = serializationOptions;
            var blockLength = blocks.Sum(x => x.Size);
            var blockInfoCount = (int) Math.Ceiling((double) blockLength / blockSize);
            var blockInfoStart = blockInfoStream.Position;
            var blockStart = blockInfoStart + 4 + blockInfoCount * 10;
            blockInfoStream.Seek(blockStart, SeekOrigin.Begin);
            UnityBundleBlock.ArrayToWriter(blockInfoWriter, blocks, header, options, serializationOptions);
            blockInfoStream.Seek(blockInfoStart, SeekOrigin.Begin);
            blockInfoWriter.Write(blockInfoCount);
            for (var i = 0; i < blockInfoCount; ++i) {
                UnityBundleBlockInfo.ToWriter(blockInfoWriter, header, options, serializationOptions, blockDataStream, blockStream);
            }

            var blockInfoSize = (int) blockInfoStream.Length;
            blockInfoStream.Seek(0, SeekOrigin.Begin);
            using var compressedStream = new MemoryStream();
            switch (unityCompressionType) {
                case UnityCompressionType.None: {
                    blockInfoStream.CopyTo(compressedStream);
                    break;
                }
                case UnityCompressionType.LZMA: {
                    Utils.EncodeLZMA(compressedStream, blockInfoStream, (int) blockInfoStream.Length);
                    break;
                }
                case UnityCompressionType.LZ4:
                case UnityCompressionType.LZ4HC: {
                    Utils.CompressLZ4(blockInfoStream, compressedStream, unityCompressionType == UnityCompressionType.LZ4HC ? LZ4Level.L12_MAX : LZ4Level.L00_FAST, (int) blockInfoStream.Length);
                    break;
                }
                default:
                    throw new NotSupportedException();
            }

            compressedStream.Seek(0, SeekOrigin.Begin);
            compressedStream.CopyTo(writer.BaseStream);
            blockDataStream.Seek(0, SeekOrigin.Begin);
            blockDataStream.CopyTo(writer.BaseStream);

            writer.BaseStream.Seek(start, SeekOrigin.Begin);
            writer.Write(writer.BaseStream.Length - start);
            writer.Write((int) compressedStream.Length);
            writer.Write(blockInfoSize);
        }

        public static UnityFS FromReader(BiEndianBinaryReader reader, UnityBundle header, EquilibriumOptions options) {
            var size = reader.ReadInt64();
            var compressedBlockSize = reader.ReadInt32();
            var blockSize = reader.ReadInt32();
            var flags = (UnityFSFlags) reader.ReadUInt32();

            var fs = new UnityFS(size, compressedBlockSize, blockSize, flags);
            if (fs.Flags.HasFlag(UnityFSFlags.BlocksInfoAtEnd)) {
                reader.BaseStream.Seek(fs.CompressedBlockInfoSize, SeekOrigin.End);
            } else if (header.FormatVersion >= 7) {
                reader.Align(16);
            }

            var compressionType = (UnityCompressionType) (fs.Flags & UnityFSFlags.CompressionRange);
            using var blocksReader = new BiEndianBinaryReader(compressionType switch {
                    UnityCompressionType.None => new OffsetStream(reader.BaseStream, length: fs.BlockInfoSize),
                    UnityCompressionType.LZMA => Utils.DecodeLZMA(reader.BaseStream, fs.CompressedBlockInfoSize, fs.BlockInfoSize),
                    UnityCompressionType.LZ4 => Utils.DecompressLZ4(reader.BaseStream, fs.CompressedBlockInfoSize, fs.BlockInfoSize),
                    UnityCompressionType.LZ4HC => Utils.DecompressLZ4(reader.BaseStream, fs.CompressedBlockInfoSize, fs.BlockInfoSize),
                    _ => throw new InvalidOperationException(),
                },
                true,
                compressionType == UnityCompressionType.None);
            blocksReader.BaseStream.Seek(0, SeekOrigin.Begin);
            fs.Hash = blocksReader.ReadBytes(16);
            var infoCount = blocksReader.ReadInt32();
            fs.BlockInfos = UnityBundleBlockInfo.ArrayFromReader(blocksReader, header, infoCount, options);
            var blockCount = blocksReader.ReadInt32();
            fs.Blocks = UnityBundleBlock.ArrayFromReader(blocksReader, header, blockCount, options);
            return fs;
        }
    }
}
