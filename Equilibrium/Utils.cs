using System;
using System.IO;
using SevenZip;
using SevenZip.Compression.LZMA;

namespace Equilibrium {
    internal static class Utils {
        internal static Stream DecodeLZMA(byte[] buffer, int compressedSize, int size, Stream? outStream = null) {
            using var inMs = new MemoryStream(buffer[5..]) { Position = 0 };
            outStream ??= new MemoryStream(size) { Position = 0 };
            var coder = new Decoder();
            coder.SetDecoderProperties(buffer[..5]);
            coder.Code(inMs, outStream, compressedSize - 5, size, null);
            return outStream;
        }

        private static CoderPropID[] PropIDs = {
            CoderPropID.DictionarySize,
            CoderPropID.PosStateBits,
            CoderPropID.LitContextBits,
            CoderPropID.LitPosBits,
            CoderPropID.Algorithm,
            CoderPropID.NumFastBytes,
            CoderPropID.MatchFinder,
            CoderPropID.EndMarker,
        };

        private static object[] Properties = {
            1 << 23,
            2,
            3,
            0,
            2,
            128,
            "bt4",
            false,
        };

        public static void EncodeLZMA(Stream outStream, Stream inStream, int size, CoderPropID[]? propIds = null, object[]? properties = null) {
            var coder = new Encoder();
            coder.SetCoderProperties(propIds, properties);
            coder.WriteCoderProperties(outStream);
            coder.Code(inStream, outStream, size, -1, null);
        }
    }
}
