using System;
using System.IO;
using System.Linq;
using Snuggle.Core.IO;
using Snuggle.Core.Models.Bundle;
using Snuggle.Core.Options;

namespace Snuggle.Core.Interfaces;

public record UnityContainer {
    public UnityBundleBlockInfo[] BlockInfos { get; protected set; } = Array.Empty<UnityBundleBlockInfo>();
    public UnityBundleBlock[] Blocks { get; protected set; } = Array.Empty<UnityBundleBlock>();
    public virtual long Length => -1;
    protected virtual long DataStart => -1;

    public Stream OpenFile(string path, BiEndianBinaryReader? reader = null) => OpenFile(Blocks.FirstOrDefault(x => x.Path.Equals(path, StringComparison.InvariantCultureIgnoreCase)), reader);

    public Stream OpenFile(UnityBundleBlock? block, BiEndianBinaryReader? reader = null) {
        if (block == null) {
            return Stream.Null;
        }

        if (reader == null) {
            throw new NotSupportedException("Cannot read file with no stream or no reader");
        }

        var stream = new MemoryStream { Position = 0 };
        if (DataStart >= 0) {
            reader.BaseStream.Seek(DataStart, SeekOrigin.Begin);
        }

        var skippedBytes = 0L;
        foreach (var (size, compressedSize, unityBundleBlockFlags) in BlockInfos) {
            if (block.Offset > stream.Length + skippedBytes + size) {
                skippedBytes += size;
                reader.BaseStream.Seek(compressedSize, SeekOrigin.Current);
                continue;
            }

            if (unityBundleBlockFlags.HasFlag(UnityBundleBlockInfoFlags.Encrypted)) {
                throw new NotSupportedException("Block is encrypted");
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
                    throw new NotSupportedException($"Unity Compression format {compressionType:G} is not supported");
            }

            if (skippedBytes + stream.Length >= block.Offset + block.Size) {
                break;
            }
        }

        return new OffsetStream(stream, block.Offset - skippedBytes, block.Size) { Position = 0 };
    }

    public virtual void ToWriter(BiEndianBinaryWriter writer, UnityBundle header, SnuggleCoreOptions options, UnityBundleBlock[] blocks, Stream blockStream, BundleSerializationOptions bundleSerializationOptions) { }
}
