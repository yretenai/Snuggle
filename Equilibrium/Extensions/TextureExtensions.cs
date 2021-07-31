﻿using System;
using DragonLib.Imaging.DXGI;
using Equilibrium.Models.Objects.Graphics;
using JetBrains.Annotations;

namespace Equilibrium.Extensions {
    [PublicAPI]
    public static class TextureExtensions {
        public static bool CanSupportDDS(this TextureFormat format) {
            return format != TextureFormat.RGB24 && format is >= TextureFormat.RG16 and <= TextureFormat.R8 or < TextureFormat.PVRTC_RGB2;
        }

        public static TextureFormat ToTextureFormat(this DXGIPixelFormat format) {
            // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
            return format switch {
                DXGIPixelFormat.A8_UNORM => TextureFormat.Alpha8,
                DXGIPixelFormat.B4G4R4A4_UNORM => TextureFormat.RGBA4444,
                DXGIPixelFormat.R8G8B8A8_UNORM => TextureFormat.RGBA32,
                DXGIPixelFormat.B8G8R8A8_UNORM => TextureFormat.BGRA32,
                DXGIPixelFormat.B5G6R5_UNORM => TextureFormat.RGB565,
                DXGIPixelFormat.R16_UNORM => TextureFormat.R16,
                DXGIPixelFormat.BC1_UNORM => TextureFormat.DXT1,
                DXGIPixelFormat.BC3_UNORM => TextureFormat.DXT5,
                DXGIPixelFormat.R16_FLOAT => TextureFormat.RHalf,
                DXGIPixelFormat.R16G16_FLOAT => TextureFormat.RGHalf,
                DXGIPixelFormat.R16G16B16A16_FLOAT => TextureFormat.RGBAHalf,
                DXGIPixelFormat.R32_FLOAT => TextureFormat.RFloat,
                DXGIPixelFormat.R32G32_FLOAT => TextureFormat.RGFloat,
                DXGIPixelFormat.R32G32B32A32_FLOAT => TextureFormat.RGBAFloat,
                (DXGIPixelFormat) 107 => TextureFormat.YUY2, // DXGI_FORMAT_YUY2
                DXGIPixelFormat.R9G9B9E5_SHAREDEXP => TextureFormat.RGB9e5Float,
                DXGIPixelFormat.BC4_UNORM => TextureFormat.BC4,
                DXGIPixelFormat.BC5_UNORM => TextureFormat.BC5,
                DXGIPixelFormat.BC6H_UF16 => TextureFormat.BC6H,
                DXGIPixelFormat.BC7_UNORM => TextureFormat.BC7,
                DXGIPixelFormat.R8G8_UNORM => TextureFormat.RG16,
                DXGIPixelFormat.R8_UNORM => TextureFormat.R8,
                _ => throw new NotSupportedException(),
            };
        }

        public static DXGIPixelFormat ToD3DPixelFormat(this TextureFormat format) {
            // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
            return format switch {
                TextureFormat.Alpha8 => DXGIPixelFormat.A8_UNORM,
                TextureFormat.ARGB4444 => DXGIPixelFormat.B4G4R4A4_UNORM,
                TextureFormat.RGBA4444 => DXGIPixelFormat.B4G4R4A4_UNORM,
                TextureFormat.RGBA32 => DXGIPixelFormat.R8G8B8A8_UNORM,
                TextureFormat.ARGB32 => DXGIPixelFormat.R8G8B8A8_UNORM,
                TextureFormat.BGRA32 => DXGIPixelFormat.B8G8R8A8_UNORM,
                TextureFormat.RGB565 => DXGIPixelFormat.B5G6R5_UNORM,
                TextureFormat.R16 => DXGIPixelFormat.R16_UNORM,
                TextureFormat.DXT1 => DXGIPixelFormat.BC1_UNORM,
                TextureFormat.DXT1Crunched => DXGIPixelFormat.BC1_UNORM,
                TextureFormat.DXT5 => DXGIPixelFormat.BC3_UNORM,
                TextureFormat.DXT5Crunched => DXGIPixelFormat.BC3_UNORM,
                TextureFormat.RHalf => DXGIPixelFormat.R16_FLOAT,
                TextureFormat.RGHalf => DXGIPixelFormat.R16G16_FLOAT,
                TextureFormat.RGBAHalf => DXGIPixelFormat.R16G16B16A16_FLOAT,
                TextureFormat.RFloat => DXGIPixelFormat.R32_FLOAT,
                TextureFormat.RGFloat => DXGIPixelFormat.R32G32_FLOAT,
                TextureFormat.RGBAFloat => DXGIPixelFormat.R32G32B32A32_FLOAT,
                TextureFormat.YUY2 => (DXGIPixelFormat) 107, // DXGI_FORMAT_YUY2
                TextureFormat.RGB9e5Float => DXGIPixelFormat.R9G9B9E5_SHAREDEXP,
                TextureFormat.BC4 => DXGIPixelFormat.BC4_UNORM,
                TextureFormat.BC5 => DXGIPixelFormat.BC5_UNORM,
                TextureFormat.BC6H => DXGIPixelFormat.BC6H_UF16,
                TextureFormat.BC7 => DXGIPixelFormat.BC7_UNORM,
                TextureFormat.RG16 => DXGIPixelFormat.R8G8_UNORM,
                TextureFormat.R8 => DXGIPixelFormat.R8_UNORM,
                _ => throw new NotSupportedException(),
            };
        }
    }
}