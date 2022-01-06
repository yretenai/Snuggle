using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using K4os.Compression.LZ4;
using SevenZip;
using SevenZip.Compression.LZMA;
using Snuggle.Core.Meta;
using Snuggle.Core.Models.Bundle;

namespace Snuggle.Core;

public static class Utils {
    private static readonly CoderPropID[] PropIDs = { CoderPropID.DictionarySize, CoderPropID.PosStateBits, CoderPropID.LitContextBits, CoderPropID.LitPosBits, CoderPropID.Algorithm, CoderPropID.NumFastBytes, CoderPropID.MatchFinder, CoderPropID.EndMarker };

    private static readonly object[] Properties = { 1 << 23, 2, 3, 0, 2, 128, "bt4", false };

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
        var inPool = new byte[compressedSize].AsSpan();
        var outPool = new byte[size].AsSpan();
        inStream.Read(inPool);
        var amount = LZ4Codec.Decode(inPool, outPool);
        outStream.Write(outPool[..amount]);
        return outStream;
    }

    public static void CompressLZ4(Stream inStream, Stream outStream, LZ4Level level, int size) {
        var inPool = new byte[size].AsSpan();
        var outPool = new byte[size].AsSpan();
        inStream.Read(inPool);
        var amount = LZ4Codec.Encode(inPool, outPool, level);
        outStream.Write(outPool[..amount]);
    }

    public static float[] UnwrapRGBA(uint rgba) {
        return new[] { (rgba & 0xFF) / (float) 0xFF, ((rgba >> 8) & 0xFF) / (float) 0xFF, ((rgba >> 16) & 0xFF) / (float) 0xFF, ((rgba >> 24) & 0xFF) / (float) 0xFF };
    }

    public static string? GetStringFromTag(object? tag) {
        while (true) {
            switch (tag) {
                case string str:
                    return str;
                case MultiMetaInfo meta:
                    tag = meta.Tag;
                    continue;
                case FileInfo fi:
                    return fi.FullName;
                case UnityBundleBlock block:
                    return block.Path;
                case null:
                    return null;
                default: {
                    Debug.WriteLine($"Unable to figure out how to unwind {tag.GetType().FullName} tag");
                    return tag.ToString();
                }
            }
        }
    }

    public static string? GetNameFromTag(object? tag) {
        var str = GetStringFromTag(tag);
        if (string.IsNullOrEmpty(str)) {
            return null;
        }

        if (Path.GetExtension(str) == ".split0") {
            return Path.GetFileNameWithoutExtension(str);
        }

        return Path.GetFileName(str);
    }

    public static string? GetNameFromTagWithoutExtension(object? tag) {
        var str = GetStringFromTag(tag);
        if (string.IsNullOrEmpty(str)) {
            return null;
        }

        return Path.GetFileNameWithoutExtension(str);
    }

    public static Memory<byte> AsBytes<T>(this Memory<T> memory) where T : struct => new Memory<byte>(MemoryMarshal.AsBytes(memory.Span).ToArray());
}
