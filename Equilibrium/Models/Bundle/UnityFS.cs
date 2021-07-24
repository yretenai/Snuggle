using System;
using System.Collections.Immutable;
using System.IO;
using DragonLib;
using Equilibrium.IO;
using JetBrains.Annotations;

namespace Equilibrium.Models.Bundle {
    [PublicAPI]
    public record UnityFS(
        long Size,
        int CompressedBlockInfoSize,
        int BlockInfoSize,
        UnityFSFlags Flags) : IUnityContainer {
        public byte[] Hash { get; set; } = Array.Empty<byte>();
        public ImmutableArray<UnityBundleBlockInfo>? BlockInfos { get; set; }
        public ImmutableArray<UnityBundleBlock>? Blocks { get; set; }

        public Span<byte> OpenFile(UnityBundleBlock? block, BiEndianBinaryReader? reader = null, Stream? stream = null) {
            if (block == null) {
                return Span<byte>.Empty;
            }

            if (stream == null) {
                if (reader == null) {
                    throw new NotSupportedException();
                }

                var streamOffset = 0L;
                var cur = -1L;
                stream = new MemoryStream();
                foreach (var (size, compressedSize, unityBundleBlockFlags) in BlockInfos ?? ImmutableArray<UnityBundleBlockInfo>.Empty) {
                    if (streamOffset + size < block.Offset) {
                        reader.BaseStream.Seek(compressedSize, SeekOrigin.Current);
                        continue;
                    }

                    if (streamOffset + size > block.Size + block.Offset) {
                        break;
                    }

                    if (cur == -1) {
                        cur = block.Offset - streamOffset;
                    }

                    Span<byte> buffer = reader.ReadBytes(compressedSize);

                    var compressionType = (UnityCompressionType) (unityBundleBlockFlags & UnityBundleBlockFlags.CompressionMask);
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
                            throw new NotImplementedException();
                    }

                    streamOffset += size;
                }

                stream.Seek((int) cur, SeekOrigin.Begin);
            } else {
                stream.Seek(block.Offset, SeekOrigin.Begin);
            }

            Span<byte> data = new byte[block.Size];
            stream.Read(data);
            return data;
        }

        public static UnityFS FromReader(BiEndianBinaryReader reader, UnityBundle header) {
            var size = reader.ReadInt64();
            var compressedBlockSize = reader.ReadInt32();
            var blockSize = reader.ReadInt32();
            var flags = (UnityFSFlags) reader.ReadUInt32();
            if (header.FormatVersion >= 7) {
                reader.Align(16);
            }

            var fs = new UnityFS(size, compressedBlockSize, blockSize, flags);
            Span<byte> blocksBuffer = new byte[fs.CompressedBlockInfoSize];
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
                UnityCompressionType.None => BiEndianBinaryReader.FromSpan(blocksBuffer, true),
                UnityCompressionType.LZMA => new BiEndianBinaryReader(Utils.DecodeLZMA(blocksBuffer, fs.CompressedBlockInfoSize, fs.BlockInfoSize), true),
                UnityCompressionType.LZ4 => BiEndianBinaryReader.FromSpan(CompressionEncryption.DecompressLZ4(blocksBuffer, fs.BlockInfoSize), true),
                UnityCompressionType.LZ4HC => BiEndianBinaryReader.FromSpan(CompressionEncryption.DecompressLZ4(blocksBuffer, fs.BlockInfoSize), true),
                _ => throw new NotImplementedException(),
            };
            fs.Hash = blocksReader.ReadBytes(16);
            var infoCount = blocksReader.ReadInt32();
            fs.BlockInfos = UnityBundleBlockInfo.ArrayFromReader(blocksReader, header, infoCount).ToImmutableArray();
            var blockCount = blocksReader.ReadInt32();
            fs.Blocks = UnityBundleBlock.ArrayFromReader(blocksReader, header, blockCount).ToImmutableArray();

            return fs;
        }
    }
}
