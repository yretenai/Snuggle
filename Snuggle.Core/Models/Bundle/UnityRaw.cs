using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Snuggle.Core.Interfaces;
using Snuggle.Core.IO;
using Snuggle.Core.Options;

namespace Snuggle.Core.Models.Bundle; 

[PublicAPI]
public record UnityRaw(
    uint Checksum,
    long MinimumStreamedBytes,
    long Size,
    int MinimumBlocks,
    long TotalSize,
    long BlockSize) : UnityContainer {
    public byte[] Hash { get; set; } = Array.Empty<byte>();
    public override long Length { get; }
    public override long DataStart => Size;

    public override void ToWriter(BiEndianBinaryWriter writer, UnityBundle header, SnuggleOptions options, UnityBundleBlock[] blocks, Stream blockStream, BundleSerializationOptions serializationOptions) {
        throw new NotImplementedException();
    }

    public static UnityRaw FromReader(BiEndianBinaryReader reader, UnityBundle header, SnuggleOptions options) {
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
        var blockInfos = UnityBundleBlockInfo.ArrayFromReader(reader, header, 0, blockInfoCount, options);
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
        unityRaw.Blocks = UnityBundleBlock.ArrayFromReader(blockReader, header, 0, blockCount, options);
        return unityRaw;
    }
}