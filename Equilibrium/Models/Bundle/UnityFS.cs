using System;
using System.Collections.Generic;
using System.IO;
using DragonLib;
using JetBrains.Annotations;

namespace Equilibrium.Models.Bundle {
    [PublicAPI]
    public record UnityFS(
        long Size,
        int CompressedBlockInfoSize,
        int BlockInfoSize,
        UnityFSFlags Flags,
        ulong Hash) : IUnityContainer {
        public ICollection<UnityBundleBlockInfo>? BlockInfos { get; set; }
        public ICollection<UnityBundleBlock>? Blocks { get; set; }

        public static UnityFS FromReader(BiEndianBinaryReader reader, UnityBundle header) {
            var size = reader.ReadInt64();
            var compressedBlockSize = reader.ReadInt32();
            var blockSize = reader.ReadInt32();
            var flags = (UnityFSFlags) reader.ReadUInt32();
            if (header.FormatVersion >= 7) {
                reader.Align(16);
            }

            Span<byte> blocksBuffer = new byte[compressedBlockSize];
            if (flags.HasFlag(UnityFSFlags.BlocksInfoAtEnd)) {
                var tmp = reader.BaseStream.Position;
                reader.BaseStream.Seek(compressedBlockSize, SeekOrigin.End);
                reader.Read(blocksBuffer);
                reader.BaseStream.Seek(tmp, SeekOrigin.Begin);
            } else {
                reader.Read(blocksBuffer);
            }

            var compressionType = (UnityFSCompressionType) (flags & UnityFSFlags.CompressionRange);
            using var blocksReader = compressionType switch {
                UnityFSCompressionType.None => BiEndianBinaryReader.FromSpan(blocksBuffer),
                UnityFSCompressionType.LZMA => new BiEndianBinaryReader(DecodeLZMA(blocksBuffer, compressedBlockSize, blockSize)),
                UnityFSCompressionType.LZ4 => BiEndianBinaryReader.FromSpan(CompressionEncryption.DecompressLZ4(blocksBuffer, blockSize),
                    true),
                UnityFSCompressionType.LZ4HC => BiEndianBinaryReader.FromSpan(CompressionEncryption.DecompressLZ4(blocksBuffer, blockSize),
                    true),
                _ => throw new NotImplementedException(),
            };
            var hash = reader.ReadUInt64();
            var fs = new UnityFS(size, compressedBlockSize, blockSize, flags, hash);
            var infoCount = reader.ReadInt32();
            fs.BlockInfos = UnityBundleBlockInfo.ArrayFromReader(blocksReader, header, infoCount);
            var blockCount = reader.ReadInt32();
            fs.Blocks = UnityBundleBlock.ArrayFromReader(blocksReader, header, blockCount);

            return fs;
        }

        private static Stream DecodeLZMA(Span<byte> blocksBuffer, int compressedBlockSize, int blockSize) {
            using var inMs = new MemoryStream(blocksBuffer[5..].ToArray()) { Position = 0 };
            var outMs = new MemoryStream(blockSize) { Position = 0 };
            var coder = new SevenZip.Compression.LZMA.Decoder();
            coder.SetDecoderProperties(blocksBuffer[..5].ToArray());
            coder.Code(inMs, outMs, compressedBlockSize - 5, blockSize, null);
            return outMs;
        }
    }
}
