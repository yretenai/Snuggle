using System;
using System.IO;
using System.Linq;
using Equilibrium.IO;
using Equilibrium.Models.Bundle;
using Equilibrium.Options;
using JetBrains.Annotations;

namespace Equilibrium.Interfaces {
    [PublicAPI]
    public record UnityContainer {
        public UnityBundleBlockInfo[] BlockInfos { get; set; } = Array.Empty<UnityBundleBlockInfo>();
        public UnityBundleBlock[] Blocks { get; set; } = Array.Empty<UnityBundleBlock>();
        public virtual long Length { get; }
        public virtual long DataStart { get; }

        public Stream OpenFile(string path, EquilibriumOptions options, BiEndianBinaryReader? reader = null, Stream? stream = null) => OpenFile(Blocks.FirstOrDefault(x => x.Path.Equals(path, StringComparison.InvariantCultureIgnoreCase)), options, reader, stream);

        public Stream OpenFile(UnityBundleBlock? block, EquilibriumOptions options, BiEndianBinaryReader? reader = null, Stream? stream = null) {
            if (block == null) {
                return Stream.Null;
            }

            if (stream != null) {
                return new OffsetStream(stream, block.Offset, block.Size, true);
            }

            if (reader == null) {
                throw new NotSupportedException("Cannot read file with no stream or no reader");
            }

            stream = new MemoryStream { Position = 0 };
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

        public virtual void ToWriter(BiEndianBinaryWriter writer, UnityBundle header, EquilibriumOptions options, UnityBundleBlock[] blocks, Stream blockStream, BundleSerializationOptions bundleSerializationOptions) { }
    }
}
