using System.IO;
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
    }
}
