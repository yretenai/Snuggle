using System;
using System.Runtime.InteropServices;

namespace Snuggle.Converters.DXGI; 

public static class DDS {
    public static Span<byte> BuildDDS(DXGIPixelFormat pixel, int mipCount, int width, int height, int frameCount, Span<byte> blob) {
        var result = new Span<byte>(new byte[blob.Length + 0x94]);
        var header = new DDSImageHeader {
            Magic = 0x2053_4444,
            Size = 0x7C,
            Flags = 0x1 | 0x2 | 0x4 | 0x1000 | 0x0002_0000,
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
                AlphaMask = 0x0000_00FF
            },
            Caps1 = 0x1008,
            Caps2 = 0,
            Caps3 = 0,
            Caps4 = 0,
            Reserved2 = 0
        };
        MemoryMarshal.Write(result, ref header);
        var dx10 = new DXT10Header {
            Format = (int)pixel,
            Dimension = DXT10ResourceDimension.TEXTURE2D,
            Misc = 0,
            Size = Math.Max(1, frameCount)
        };
        MemoryMarshal.Write(result[0x80..], ref dx10);
        blob.CopyTo(result[0x94..]);
        return result;
    }

    public static Span<byte> BGRARGBA(Span<byte> bgra) {
        var value = new Span<byte>(new byte[bgra.Length]);
        for (var i = 0; i < bgra.Length; i += 4) {
            value[i + 0] = bgra[i + 2];
            value[i + 1] = bgra[i + 1];
            value[i + 2] = bgra[i + 0];
            value[i + 3] = bgra[i + 3];
        }

        return value;
    }

    public static Span<byte> R8G8(Span<byte> bgra) {
        var value = new Span<byte>(new byte[bgra.Length * 2]);
        for (var i = 0; i < bgra.Length; i += 2) {
            value[i * 2 + 0] = bgra[i + 0];
            value[i * 2 + 1] = bgra[i + 1];
        }

        return value;
    }

    public static Span<byte> DecompressDXGIFormat(Span<byte> data, int width, int height, DXGIPixelFormat format) {
        // ReSharper disable once SwitchStatementMissingSomeCases
        switch (format) {
            case DXGIPixelFormat.R8G8B8A8_SINT:
            case DXGIPixelFormat.R8G8B8A8_UINT:
            case DXGIPixelFormat.R8G8B8A8_UNORM:
            case DXGIPixelFormat.R8G8B8A8_UNORM_SRGB:
            case DXGIPixelFormat.R8G8B8A8_SNORM:
            case DXGIPixelFormat.R8G8B8A8_TYPELESS:
                return data;
            case DXGIPixelFormat.B8G8R8A8_UNORM:
            case DXGIPixelFormat.B8G8R8A8_UNORM_SRGB:
            case DXGIPixelFormat.B8G8R8A8_TYPELESS:
                return BGRARGBA(data);
            case DXGIPixelFormat.R8G8_SINT:
            case DXGIPixelFormat.R8G8_UINT:
            case DXGIPixelFormat.R8G8_SNORM:
            case DXGIPixelFormat.R8G8_UNORM:
            case DXGIPixelFormat.R8G8_TYPELESS:
                return R8G8(data);
            case DXGIPixelFormat.BC1_TYPELESS:
            case DXGIPixelFormat.BC1_UNORM:
            case DXGIPixelFormat.BC1_UNORM_SRGB: {
                throw new NotImplementedException();
            }
            case DXGIPixelFormat.BC2_TYPELESS:
            case DXGIPixelFormat.BC2_UNORM:
            case DXGIPixelFormat.BC2_UNORM_SRGB: {
                throw new NotImplementedException();
            }
            case DXGIPixelFormat.BC3_TYPELESS:
            case DXGIPixelFormat.BC3_UNORM:
            case DXGIPixelFormat.BC3_UNORM_SRGB: {
                throw new NotImplementedException();
            }
            default:
                throw new InvalidOperationException($"Format {format} is not supported yet!");
        }
    }
}
