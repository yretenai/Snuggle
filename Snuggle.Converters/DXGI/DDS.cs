using System;
using System.Runtime.InteropServices;

namespace Snuggle.Converters.DXGI;

public static class DDS {
    public static Span<byte> BuildDDS(DXGIPixelFormat pixel, int mipCount, int width, int height, int frameCount, Span<byte> blob) {
        var result = new Span<byte>(new byte[blob.Length + 0x94]);
        var header = new DDSImageHeader {
            Magic = 0x2053_4444,
            Size = 0x7C,
            Flags = 0x1 | 0x2 | 0x4 | 0x1000 | 0x20000,
            Width = width,
            Height = height,
            LinearSize = 0,
            Depth = 0,
            MipmapCount = mipCount,
            Format = new DDSPixelFormat {
                Size = 0x20,
                Flags = 4,
                FourCC = 0x3031_5844,
                BitCount = 0x20,
                RedMask = 0x0000_FF00,
                GreenMask = 0x00FF_0000,
                BlueMask = 0xFF00_0000,
                AlphaMask = 0x0000_00FF,
            },
            Caps1 = 0x8 | 0x1000 | 0x400000,
            Caps2 = 0,
            Caps3 = 0,
            Caps4 = 0,
            Reserved2 = 0,
        };
        MemoryMarshal.Write(result, ref header);
        var dx10 = new DXT10Header {
            Format = (int) pixel, Dimension = DXT10ResourceDimension.TEXTURE2D, Misc = 0, Size = frameCount,
        };
        MemoryMarshal.Write(result[0x80..], ref dx10);
        blob.CopyTo(result[0x94..]);
        return result;
    }
}
