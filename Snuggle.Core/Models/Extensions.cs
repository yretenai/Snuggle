using System;
using Snuggle.Core.Exceptions;
using Snuggle.Core.Interfaces;
using Snuggle.Core.Models.Objects.Graphics;

namespace Snuggle.Core.Models;

public static class Extensions {
    public static TextureFormat ToTextureFormat(this GraphicsFormat format) {
        switch (format) {
            case GraphicsFormat.None:
                return TextureFormat.None;
            case GraphicsFormat.R8_SRGB:
            case GraphicsFormat.R8_UNorm:
            case GraphicsFormat.R8_SNorm:
            case GraphicsFormat.R8_UInt:
            case GraphicsFormat.R8_SInt:
                return TextureFormat.R8;
            case GraphicsFormat.R8G8_SRGB:
            case GraphicsFormat.R8G8_UNorm:
            case GraphicsFormat.R8G8_SNorm:
            case GraphicsFormat.R8G8_UInt:
            case GraphicsFormat.R8G8_SInt:
                return TextureFormat.RG16;
            case GraphicsFormat.R8G8B8_SRGB:
            case GraphicsFormat.R8G8B8_UNorm:
            case GraphicsFormat.R8G8B8_SNorm:
            case GraphicsFormat.R8G8B8_UInt:
            case GraphicsFormat.R8G8B8_SInt:
                return TextureFormat.RGB24;
            case GraphicsFormat.R8G8B8A8_SRGB:
            case GraphicsFormat.R8G8B8A8_UNorm:
            case GraphicsFormat.R8G8B8A8_SNorm:
            case GraphicsFormat.R8G8B8A8_UInt:
            case GraphicsFormat.R8G8B8A8_SInt:
                return TextureFormat.RGBA32;
            case GraphicsFormat.R16_UNorm:
            case GraphicsFormat.R16_SNorm:
            case GraphicsFormat.R16_UInt:
            case GraphicsFormat.R16_SInt:
                return TextureFormat.R16;
            case GraphicsFormat.R16G16_UNorm:
            case GraphicsFormat.R16G16_SNorm:
            case GraphicsFormat.R16G16_UInt:
            case GraphicsFormat.R16G16_SInt:
                return TextureFormat.RG32;
            case GraphicsFormat.R16G16B16_UNorm:
            case GraphicsFormat.R16G16B16_SNorm:
            case GraphicsFormat.R16G16B16_UInt:
            case GraphicsFormat.R16G16B16_SInt:
                return TextureFormat.RGB48;
            case GraphicsFormat.R16G16B16A16_UNorm:
            case GraphicsFormat.R16G16B16A16_SNorm:
            case GraphicsFormat.R16G16B16A16_UInt:
            case GraphicsFormat.R16G16B16A16_SInt:
                return TextureFormat.RGBA64;
            case GraphicsFormat.R32_UInt:
            case GraphicsFormat.R32_SInt:
                return TextureFormat.R16;
            case GraphicsFormat.R32G32_UInt:
            case GraphicsFormat.R32G32_SInt:
                throw new NotSupportedException();
            case GraphicsFormat.R32G32B32_UInt:
            case GraphicsFormat.R32G32B32_SInt:
                throw new NotSupportedException();
            case GraphicsFormat.R32G32B32A32_UInt:
            case GraphicsFormat.R32G32B32A32_SInt:
                throw new NotSupportedException();
            case GraphicsFormat.R16_SFloat:
                return TextureFormat.RHalf;
            case GraphicsFormat.R16G16_SFloat:
                return TextureFormat.RGHalf;
            case GraphicsFormat.R16G16B16_SFloat:
                throw new NotSupportedException();
            case GraphicsFormat.R16G16B16A16_SFloat:
                return TextureFormat.RGBAHalf;
            case GraphicsFormat.R32_SFloat:
                return TextureFormat.RFloat;
            case GraphicsFormat.R32G32_SFloat:
                return TextureFormat.RGFloat;
            case GraphicsFormat.R32G32B32_SFloat:
                throw new NotSupportedException();
            case GraphicsFormat.R32G32B32A32_SFloat:
                return TextureFormat.RGBAFloat;
            case GraphicsFormat.B8G8R8_SRGB:
            case GraphicsFormat.B8G8R8_UNorm:
            case GraphicsFormat.B8G8R8_SNorm:
            case GraphicsFormat.B8G8R8_UInt:
            case GraphicsFormat.B8G8R8_SInt:
                return TextureFormat.RGB24;
            case GraphicsFormat.B8G8R8A8_SRGB:
            case GraphicsFormat.B8G8R8A8_UNorm:
            case GraphicsFormat.B8G8R8A8_SNorm:
            case GraphicsFormat.B8G8R8A8_UInt:
            case GraphicsFormat.B8G8R8A8_SInt:
                return TextureFormat.BGRA32;
            case GraphicsFormat.R4G4B4A4_UNormPack16:
            case GraphicsFormat.B4G4R4A4_UNormPack16:
                return TextureFormat.RGBA4444;
            case GraphicsFormat.R5G6B5_UNormPack16:
            case GraphicsFormat.B5G6R5_UNormPack16:
                return TextureFormat.RGB565;
            case GraphicsFormat.R5G5B5A1_UNormPack16:
                throw new NotSupportedException();
            case GraphicsFormat.B5G5R5A1_UNormPack16:
                throw new NotSupportedException();
            case GraphicsFormat.A1R5G5B5_UNormPack16:
                throw new NotSupportedException();
            case GraphicsFormat.E5B9G9R9_UFloatPack32:
                return TextureFormat.RGB9e5Float;
            case GraphicsFormat.B10G11R11_UFloatPack32:
                throw new NotSupportedException();
            case GraphicsFormat.A2B10G10R10_UNormPack32:
                throw new NotSupportedException();
            case GraphicsFormat.A2B10G10R10_UIntPack32:
                throw new NotSupportedException();
            case GraphicsFormat.A2B10G10R10_SIntPack32:
                throw new NotSupportedException();
            case GraphicsFormat.A2R10G10B10_UNormPack32:
                throw new NotSupportedException();
            case GraphicsFormat.A2R10G10B10_UIntPack32:
                throw new NotSupportedException();
            case GraphicsFormat.A2R10G10B10_SIntPack32:
                throw new NotSupportedException();
            case GraphicsFormat.A2R10G10B10_XRSRGBPack32:
                throw new NotSupportedException();
            case GraphicsFormat.A2R10G10B10_XRUNormPack32:
                throw new NotSupportedException();
            case GraphicsFormat.R10G10B10_XRSRGBPack32:
                throw new NotSupportedException();
            case GraphicsFormat.R10G10B10_XRUNormPack32:
                throw new NotSupportedException();
            case GraphicsFormat.A10R10G10B10_XRSRGBPack32:
                throw new NotSupportedException();
            case GraphicsFormat.A10R10G10B10_XRUNormPack32:
                throw new NotSupportedException();
            case GraphicsFormat.D16_UNorm:
                throw new NotSupportedException();
            case GraphicsFormat.D24_UNorm:
                throw new NotSupportedException();
            case GraphicsFormat.D24_UNorm_S8_UInt:
                throw new NotSupportedException();
            case GraphicsFormat.D32_SFloat:
                throw new NotSupportedException();
            case GraphicsFormat.D32_SFloat_S8_UInt:
                throw new NotSupportedException();
            case GraphicsFormat.S8_UInt:
                return TextureFormat.Alpha8;
            case GraphicsFormat.RGB_DXT1_SRGB:
            case GraphicsFormat.RGB_DXT1_UNorm:
                return TextureFormat.DXT1;
            case GraphicsFormat.RGBA_DXT3_SRGB:
                throw new NotSupportedException();
            case GraphicsFormat.RGBA_DXT3_UNorm:
                throw new NotSupportedException();
            case GraphicsFormat.RGBA_DXT5_SRGB:
            case GraphicsFormat.RGBA_DXT5_UNorm:
                return TextureFormat.DXT5;
            case GraphicsFormat.R_BC4_UNorm:
            case GraphicsFormat.R_BC4_SNorm:
                return TextureFormat.BC4;
            case GraphicsFormat.RG_BC5_UNorm:
            case GraphicsFormat.RG_BC5_SNorm:
                return TextureFormat.BC5;
            case GraphicsFormat.RGB_BC6H_UFloat:
            case GraphicsFormat.RGB_BC6H_SFloat:
                return TextureFormat.BC6H;
            case GraphicsFormat.RGBA_BC7_SRGB:
            case GraphicsFormat.RGBA_BC7_UNorm:
                return TextureFormat.BC7;
            case GraphicsFormat.RGB_PVRTC_2Bpp_SRGB:
            case GraphicsFormat.RGB_PVRTC_2Bpp_UNorm:
                return TextureFormat.PVRTC_RGB2;
            case GraphicsFormat.RGB_PVRTC_4Bpp_SRGB:
            case GraphicsFormat.RGB_PVRTC_4Bpp_UNorm:
                return TextureFormat.PVRTC_RGB4;
            case GraphicsFormat.RGBA_PVRTC_2Bpp_SRGB:
            case GraphicsFormat.RGBA_PVRTC_2Bpp_UNorm:
                return TextureFormat.PVRTC_RGBA2;
            case GraphicsFormat.RGBA_PVRTC_4Bpp_SRGB:
            case GraphicsFormat.RGBA_PVRTC_4Bpp_UNorm:
                return TextureFormat.PVRTC_RGBA4;
            case GraphicsFormat.RGB_ETC_UNorm:
                return TextureFormat.ETC_RGB4;
            case GraphicsFormat.RGB_ETC2_SRGB:
            case GraphicsFormat.RGB_ETC2_UNorm:
                return TextureFormat.ETC2_RGB;
            case GraphicsFormat.RGB_A1_ETC2_SRGB:
            case GraphicsFormat.RGB_A1_ETC2_UNorm:
                return TextureFormat.ETC2_RGBA1;
            case GraphicsFormat.RGBA_ETC2_SRGB:
            case GraphicsFormat.RGBA_ETC2_UNorm:
                return TextureFormat.ETC2_RGBA8;
            case GraphicsFormat.R_EAC_UNorm:
                return TextureFormat.EAC_R;
            case GraphicsFormat.R_EAC_SNorm:
                return TextureFormat.EAC_R_SIGNED;
            case GraphicsFormat.RG_EAC_UNorm:
                return TextureFormat.EAC_RG;
            case GraphicsFormat.RG_EAC_SNorm:
                return TextureFormat.EAC_RG_SIGNED;
            case GraphicsFormat.RGBA_ASTC4X4_SRGB:
            case GraphicsFormat.RGBA_ASTC4X4_UNorm:
                return TextureFormat.ASTC_RGBA_4x4;
            case GraphicsFormat.RGBA_ASTC5X5_SRGB:
            case GraphicsFormat.RGBA_ASTC5X5_UNorm:
                return TextureFormat.ASTC_RGBA_5x5;
            case GraphicsFormat.RGBA_ASTC6X6_SRGB:
            case GraphicsFormat.RGBA_ASTC6X6_UNorm:
                return TextureFormat.ASTC_RGBA_6x6;
            case GraphicsFormat.RGBA_ASTC8X8_SRGB:
            case GraphicsFormat.RGBA_ASTC8X8_UNorm:
                return TextureFormat.ASTC_RGBA_8x8;
            case GraphicsFormat.RGBA_ASTC10X10_SRGB:
            case GraphicsFormat.RGBA_ASTC10X10_UNorm:
                return TextureFormat.ASTC_RGBA_10x10;
            case GraphicsFormat.RGBA_ASTC12X12_SRGB:
            case GraphicsFormat.RGBA_ASTC12X12_UNorm:
                return TextureFormat.ASTC_RGBA_12x12;
            case GraphicsFormat.YUV2:
                return TextureFormat.YUV2;
            case GraphicsFormat.DepthAuto:
                throw new NotSupportedException();
            case GraphicsFormat.ShadowAuto:
                throw new NotSupportedException();
            case GraphicsFormat.VideoAuto:
                throw new NotSupportedException();
            case GraphicsFormat.RGBA_ASTC4X4_UFloat:
                return TextureFormat.ASTC_HDR_4x4;
            case GraphicsFormat.RGBA_ASTC5X5_UFloat:
                return TextureFormat.ASTC_HDR_5x5;
            case GraphicsFormat.RGBA_ASTC6X6_UFloat:
                return TextureFormat.ASTC_HDR_6x6;
            case GraphicsFormat.RGBA_ASTC8X8_UFloat:
                return TextureFormat.ASTC_HDR_8x8;
            case GraphicsFormat.RGBA_ASTC10X10_UFloat:
                return TextureFormat.ASTC_HDR_10x10;
            case GraphicsFormat.RGBA_ASTC12X12_UFloat:
                return TextureFormat.ASTC_HDR_12x12;
            case GraphicsFormat.D16_UNorm_S8_UInt:
                throw new NotSupportedException();
            default:
                throw new NotSupportedException();
        }
    }

    public static int GetPitch(this ITexture texture) {
        if (texture.ShouldDeserialize) {
            throw new IncompleteDeserialization();
        }

        if (texture.Depth == 1) {
            return -1;
        }

        if (texture.Width % 4 > 0 || texture.Height % 4 > 0) {
            return -1;
        }

        int bitsPerPixel;
        var pixelsPerBlock = 1;

        switch (texture.TextureFormat) {
            case TextureFormat.Alpha8:
                bitsPerPixel = 8;
                break;
            case TextureFormat.ARGB4444:
                bitsPerPixel = 16;
                break;
            case TextureFormat.RGB24:
                bitsPerPixel = 24;
                break;
            case TextureFormat.RGBA32:
                bitsPerPixel = 32;
                break;
            case TextureFormat.ARGB32:
                bitsPerPixel = 32;
                break;
            case TextureFormat.RGB565:
                bitsPerPixel = 16;
                break;
            case TextureFormat.R16:
                bitsPerPixel = 16;
                break;
            case TextureFormat.DXT1:
                bitsPerPixel = 64;
                pixelsPerBlock = 16;
                break;
            case TextureFormat.DXT5:
                bitsPerPixel = 128;
                pixelsPerBlock = 16;
                break;
            case TextureFormat.RGBA4444:
                bitsPerPixel = 16;
                break;
            case TextureFormat.BGRA32:
                bitsPerPixel = 32;
                break;
            case TextureFormat.RHalf:
                bitsPerPixel = 16;
                break;
            case TextureFormat.RGHalf:
                bitsPerPixel = 32;
                break;
            case TextureFormat.RGBAHalf:
                bitsPerPixel = 64;
                break;
            case TextureFormat.RFloat:
                bitsPerPixel = 32;
                break;
            case TextureFormat.RGFloat:
                bitsPerPixel = 64;
                break;
            case TextureFormat.RGBAFloat:
                bitsPerPixel = 128;
                break;
            case TextureFormat.YUV2:
                bitsPerPixel = 32;
                break;
            case TextureFormat.RGB9e5Float:
                bitsPerPixel = 32;
                break;
            case TextureFormat.BC6H:
                bitsPerPixel = 128;
                pixelsPerBlock = 16;
                break;
            case TextureFormat.BC7:
                bitsPerPixel = 128;
                pixelsPerBlock = 16;
                break;
            case TextureFormat.BC4:
                bitsPerPixel = 64;
                pixelsPerBlock = 16;
                break;
            case TextureFormat.BC5:
                bitsPerPixel = 128;
                pixelsPerBlock = 16;
                break;
            case TextureFormat.RG16:
                bitsPerPixel = 32;
                break;
            case TextureFormat.R8:
                bitsPerPixel = 8;
                break;
            case TextureFormat.RG32:
                bitsPerPixel = 32;
                break;
            case TextureFormat.RGB48:
                bitsPerPixel = 48;
                break;
            case TextureFormat.RGBA64:
                bitsPerPixel = 64;
                break;
            default:
                return -1;
        }

        var w = texture.Width;
        var h = texture.Height;

        var size = 0;
        for (var i = 0; i < texture.MipCount; ++i) { // TODO: is this optimizeable?
            size += w * h / pixelsPerBlock * bitsPerPixel / 8;
            w >>= 1;
            h >>= 1;
        }

        return size;
    }
}
