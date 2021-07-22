using System;
using System.IO;
using SevenZip.Compression.LZMA;

namespace Equilibrium {
    internal static class Utils {
        internal static Stream DecodeLZMA(Span<byte> buffer, int compressedSize, int size, Stream? outStream = null) {
            using var inMs = new MemoryStream(buffer[5..].ToArray()) { Position = 0 };
            outStream ??= new MemoryStream(size) { Position = 0 };
            var coder = new Decoder();
            coder.SetDecoderProperties(buffer[..5].ToArray());
            coder.Code(inMs, outStream, compressedSize - 5, size, null);
            return outStream;
        }
    }
}
