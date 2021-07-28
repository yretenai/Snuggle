using System;
using System.Buffers;
using System.IO;
using K4os.Compression.LZ4;
using SevenZip;
using SevenZip.Compression.LZMA;

namespace Equilibrium {
    internal static class Utils {
        private static readonly CoderPropID[] PropIDs = {
            CoderPropID.DictionarySize,
            CoderPropID.PosStateBits,
            CoderPropID.LitContextBits,
            CoderPropID.LitPosBits,
            CoderPropID.Algorithm,
            CoderPropID.NumFastBytes,
            CoderPropID.MatchFinder,
            CoderPropID.EndMarker,
        };

        private static readonly object[] Properties = {
            1 << 23,
            2,
            3,
            0,
            2,
            128,
            "bt4",
            false,
        };

        internal static Stream DecodeLZMA(Stream inStream, int compressedSize, int size, Stream? outStream = null) {
            outStream ??= new MemoryStream(size) { Position = 0 };
            var coder = new Decoder();
            Span<byte> properties = stackalloc byte[5];
            inStream.Read(properties);
            coder.SetDecoderProperties(properties.ToArray());
            coder.Code(inStream, outStream, compressedSize - 5, size, null);
            return outStream;
        }

        public static void EncodeLZMA(Stream outStream, Stream inStream, int size, CoderPropID[]? propIds = null, object[]? properties = null) {
            var coder = new Encoder();
            coder.SetCoderProperties(propIds ?? PropIDs, properties ?? Properties);
            coder.WriteCoderProperties(outStream);
            coder.Code(inStream, outStream, size, -1, null);
        }

        public static Stream DecompressLZ4(Stream inStream, int compressedSize, int size, Stream? outStream = null) {
            outStream ??= new MemoryStream(size) { Position = 0 };
            var inPool = ArrayPool<byte>.Shared.Rent(compressedSize);
            try {
                var outPool = ArrayPool<byte>.Shared.Rent(size);
                try {
                    inStream.Read(inPool.AsSpan()[..compressedSize]);
                    var amount = LZ4Codec.Decode(inPool.AsSpan()[..compressedSize], outPool.AsSpan()[..size]);
                    outStream.Write(outPool.AsSpan()[..amount]);
                } finally {
                    ArrayPool<byte>.Shared.Return(outPool);
                }
            } finally {
                ArrayPool<byte>.Shared.Return(inPool);
            }

            return outStream;
        }

        public static void CompressLZ4(Stream inStream, Stream outStream, LZ4Level level, int size) {
            var inPool = ArrayPool<byte>.Shared.Rent(size);
            try {
                var outPool = ArrayPool<byte>.Shared.Rent(size);
                try {
                    inStream.Read(inPool.AsSpan()[..size]);
                    var amount = LZ4Codec.Encode(inPool.AsSpan()[..size], outPool, level);
                    outStream.Write(outPool.AsSpan()[..amount]);
                } finally {
                    ArrayPool<byte>.Shared.Return(outPool);
                }
            } finally {
                ArrayPool<byte>.Shared.Return(inPool);
            }
        }
    }
}
