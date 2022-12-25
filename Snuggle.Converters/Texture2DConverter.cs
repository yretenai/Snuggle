using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AssetRipper.TextureDecoder.Astc;
using AssetRipper.TextureDecoder.Atc;
using AssetRipper.TextureDecoder.Bc;
using AssetRipper.TextureDecoder.Etc;
using AssetRipper.TextureDecoder.Pvrtc;
using AssetRipper.TextureDecoder.Rgb;
using AssetRipper.TextureDecoder.Rgb.Formats;
using AssetRipper.TextureDecoder.Yuy2;
using CommunityToolkit.HighPerformance.Buffers;
using Snuggle.Converters.DXGI;
using Snuggle.Core;
using Snuggle.Core.Exceptions;
using Snuggle.Core.Implementations;
using Snuggle.Core.Interfaces;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models;
using Snuggle.Core.Models.Objects.Graphics;
using Snuggle.Core.Models.Serialization;
using Texture2DDecoder;

namespace Snuggle.Converters;

public static class Texture2DConverter {
    public static bool SupportsDDS(ITexture texture) => texture.TextureFormat.CanSupportDDS();
    public static bool UseDDSConversion(TextureFormat textureFormat) => textureFormat.CanSupportDDS();

    public static MemoryOwner<byte> ToPixels(ITexture texture) {
        if (texture.TextureData!.Value.IsEmpty) {
            return MemoryOwner<byte>.Empty;
        }

        var srcPitch = texture.GetPitch();
        var dstPitch = texture.Width * texture.Height * 4;
        var textureData = texture.TextureData.Value;
        var frames = srcPitch > 0 ? texture.Depth : 1;
        var tex = MemoryOwner<byte>.Allocate(dstPitch * frames);
        for (var i = 0; i < frames; ++i) {
            DecodeFrame(texture, textureData[(srcPitch * i)..], tex.Memory[(dstPitch * i)..]);
        }

        return tex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WrapRgbConverter<InColor, InType>(Memory<byte> textureMem, Memory<byte> imageData) where InColor : unmanaged, IColor<InType> where InType : unmanaged {
        RgbConverter.Convert<InColor, InType, ColorBGRA32, byte>(MemoryMarshal.Cast<byte, InColor>(textureMem.Span), MemoryMarshal.Cast<byte, ColorBGRA32>(imageData.Span));
    }

    private static void DecodeFrame(ITexture texture, Memory<byte> textureMem, Memory<byte> imageData) {
        switch (texture.TextureFormat) {
            case TextureFormat.Alpha8:
                WrapRgbConverter<ColorA8, byte>(textureMem, imageData);
                break;
            case TextureFormat.ARGB4444:
                WrapRgbConverter<ColorARGB16, byte>(textureMem, imageData);
                break;
            case TextureFormat.R8:
                WrapRgbConverter<ColorR8, byte>(textureMem, imageData);
                break;
            case TextureFormat.RG16:
                WrapRgbConverter<ColorRG16, byte>(textureMem, imageData);
                break;
            case TextureFormat.RGB24:
                WrapRgbConverter<ColorRGB24, byte>(textureMem, imageData);
                break;
            case TextureFormat.RGBA32:
                WrapRgbConverter<ColorRGBA32, byte>(textureMem, imageData);
                break;
            case TextureFormat.R8_SIGNED:
                WrapRgbConverter<ColorR8Signed, sbyte>(textureMem, imageData);
                break;
            case TextureFormat.RG16_SIGNED:
                WrapRgbConverter<ColorRG16Signed, sbyte>(textureMem, imageData);
                break;
            case TextureFormat.RGB24_SIGNED:
                WrapRgbConverter<ColorRGB24Signed, sbyte>(textureMem, imageData);
                break;
            case TextureFormat.RGBA32_SIGNED:
                WrapRgbConverter<ColorRGBA32Signed, sbyte>(textureMem, imageData);
                break;
            case TextureFormat.BGRA32:
                textureMem[..imageData.Length].CopyTo(imageData);
                break;
            case TextureFormat.ARGB32:
                WrapRgbConverter<ColorARGB32, byte>(textureMem, imageData);
                break;
            case TextureFormat.RGB565:
                WrapRgbConverter<ColorRGB16, byte>(textureMem, imageData);
                break;
            case TextureFormat.R16:
                WrapRgbConverter<ColorR16, ushort>(textureMem, imageData);
                break;
            case TextureFormat.RG32:
                WrapRgbConverter<ColorRG32, ushort>(textureMem, imageData);
                break;
            case TextureFormat.RGB48:
                WrapRgbConverter<ColorRGB48, ushort>(textureMem, imageData);
                break;
            case TextureFormat.RGBA64:
                WrapRgbConverter<ColorRGBA64, ushort>(textureMem, imageData);
                break;
            case TextureFormat.R16_SIGNED:
                WrapRgbConverter<ColorR16Signed, short>(textureMem, imageData);
                break;
            case TextureFormat.RG32_SIGNED:
                WrapRgbConverter<ColorRG32Signed, short>(textureMem, imageData);
                break;
            case TextureFormat.RGB48_SIGNED:
                WrapRgbConverter<ColorRGB48Signed, short>(textureMem, imageData);
                break;
            case TextureFormat.RGBA64_SIGNED:
                WrapRgbConverter<ColorRGBA64Signed, short>(textureMem, imageData);
                break;
            case TextureFormat.RGBA4444:
                WrapRgbConverter<ColorRGBA16, byte>(textureMem, imageData);
                break;
            case TextureFormat.RHalf:
                WrapRgbConverter<ColorR16Half, Half>(textureMem, imageData);
                break;
            case TextureFormat.RGHalf:
                WrapRgbConverter<ColorRG32Half, Half>(textureMem, imageData);
                break;
            case TextureFormat.RGBAHalf:
                WrapRgbConverter<ColorRGBA64Half, Half>(textureMem, imageData);
                break;
            case TextureFormat.RFloat:
                WrapRgbConverter<ColorR32Single, float>(textureMem, imageData);
                break;
            case TextureFormat.RGFloat:
                WrapRgbConverter<ColorRG64Single, float>(textureMem, imageData);
                break;
            case TextureFormat.RGBAFloat:
                WrapRgbConverter<ColorRGBA128Single, float>(textureMem, imageData);
                break;
            case TextureFormat.RGB9e5Float:
                WrapRgbConverter<ColorRGB9e5, double>(textureMem, imageData);
                break;
            case TextureFormat.YUV2:
                Yuy2Decoder.DecompressYUY2<ColorBGRA32, byte>(textureMem.Span, texture.Width, texture.Height, imageData.Span);
                break;
            case TextureFormat.DXT1: {
                BcDecoder.DecompressBC1(textureMem.Span, texture.Width, texture.Height, imageData.Span);
                break;
            }
            case TextureFormat.DXT1Crunched when UnpackCrunch(texture.SerializedFile.Version,
                texture.TextureFormat,
                textureMem.Span,
                out var data): {
                BcDecoder.DecompressBC1(data, texture.Width, texture.Height, imageData.Span);
                break;
            }
            case TextureFormat.DXT5: {
                BcDecoder.DecompressBC3(textureMem.Span, texture.Width, texture.Height, imageData.Span);
                break;
            }
            case TextureFormat.DXT5Crunched when UnpackCrunch(texture.SerializedFile.Version,
                texture.TextureFormat,
                textureMem.Span,
                out var data): {
                BcDecoder.DecompressBC3(data, texture.Width, texture.Height, imageData.Span);
                break;
            }
            case TextureFormat.BC4: {
                BcDecoder.DecompressBC4(textureMem.Span, texture.Width, texture.Height, imageData.Span);
                break;
            }
            case TextureFormat.BC5: {
                BcDecoder.DecompressBC5(textureMem.Span, texture.Width, texture.Height, imageData.Span);
                break;
            }
            case TextureFormat.BC6H: {
                BcDecoder.DecompressBC6H(textureMem.Span, texture.Width, texture.Height, false, imageData.Span);
                break;
            }
            case TextureFormat.BC7: {
                BcDecoder.DecompressBC7(textureMem.Span, texture.Width, texture.Height, imageData.Span);
                break;
            }
            case TextureFormat.PVRTC_RGB2:
            case TextureFormat.PVRTC_RGBA2:
                PvrtcDecoder.DecompressPVRTC(textureMem.Span, texture.Width, texture.Height, true, imageData.Span);
                break;
            case TextureFormat.PVRTC_RGB4:
            case TextureFormat.PVRTC_RGBA4:
                PvrtcDecoder.DecompressPVRTC(textureMem.Span, texture.Width, texture.Height, false, imageData.Span);
                break;
            case TextureFormat.ATC_RGB4:
                AtcDecoder.DecompressAtcRgb4(textureMem.Span, texture.Width, texture.Height, imageData.Span);
                break;
            case TextureFormat.ATC_RGBA8:
                AtcDecoder.DecompressAtcRgba8(textureMem.Span, texture.Width, texture.Height, imageData.Span);
                break;
            case TextureFormat.ETC_RGB4:
            case TextureFormat.ETC_RGB4_3DS:
                EtcDecoder.DecompressETC(textureMem.Span, texture.Width, texture.Height, imageData.Span);
                break;
            case TextureFormat.ETC_RGB4Crunched when UnpackCrunch(texture.SerializedFile.Version,
                texture.TextureFormat,
                textureMem.Span,
                out var data): {
                EtcDecoder.DecompressETC(data, texture.Width, texture.Height, imageData.Span);
                break;
            }
            case TextureFormat.EAC_R:
                EtcDecoder.DecompressEACRUnsigned(textureMem.Span, texture.Width, texture.Height, imageData.Span);
                break;
            case TextureFormat.EAC_R_SIGNED:
                EtcDecoder.DecompressEACRSigned(textureMem.Span, texture.Width, texture.Height, imageData.Span);
                break;
            case TextureFormat.EAC_RG:
                EtcDecoder.DecompressEACRGUnsigned(textureMem.Span, texture.Width, texture.Height, imageData.Span);
                break;
            case TextureFormat.EAC_RG_SIGNED:
                EtcDecoder.DecompressEACRGSigned(textureMem.Span, texture.Width, texture.Height, imageData.Span);
                break;
            case TextureFormat.ETC2_RGBA8_3DS:
            case TextureFormat.ETC2_RGB:
                EtcDecoder.DecompressETC2(textureMem.Span, texture.Width, texture.Height, imageData.Span);
                break;
            case TextureFormat.ETC2_RGBA1:
                EtcDecoder.DecompressETC2A1(textureMem.Span, texture.Width, texture.Height, imageData.Span);
                break;
            case TextureFormat.ETC2_RGBA8:
                EtcDecoder.DecompressETC2A8(textureMem.Span, texture.Width, texture.Height, imageData.Span);
                break;
            case TextureFormat.ETC2_RGBA8Crunched when UnpackCrunch(texture.SerializedFile.Version,
                texture.TextureFormat,
                textureMem.Span,
                out var data): {
                EtcDecoder.DecompressETC2A8(data, texture.Width, texture.Height, imageData.Span);
                break;
            }
            case TextureFormat.ASTC_4x4:
            case TextureFormat.ASTC_RGBA_4x4:
            case TextureFormat.ASTC_HDR_4x4:
                AstcDecoder.DecodeASTC(textureMem.Span, texture.Width, texture.Height, 4, 4, imageData.Span);
                break;
            case TextureFormat.ASTC_5x5:
            case TextureFormat.ASTC_RGBA_5x5:
            case TextureFormat.ASTC_HDR_5x5:
                AstcDecoder.DecodeASTC(textureMem.Span, texture.Width, texture.Height, 5, 5, imageData.Span);
                break;
            case TextureFormat.ASTC_6x6:
            case TextureFormat.ASTC_RGBA_6x6:
            case TextureFormat.ASTC_HDR_6x6:
                AstcDecoder.DecodeASTC(textureMem.Span, texture.Width, texture.Height, 6, 6, imageData.Span);
                break;
            case TextureFormat.ASTC_8x8:
            case TextureFormat.ASTC_RGBA_8x8:
            case TextureFormat.ASTC_HDR_8x8:
                AstcDecoder.DecodeASTC(textureMem.Span, texture.Width, texture.Height, 8, 8, imageData.Span);
                break;
            case TextureFormat.ASTC_10x10:
            case TextureFormat.ASTC_RGBA_10x10:
            case TextureFormat.ASTC_HDR_10x10:
                AstcDecoder.DecodeASTC(textureMem.Span, texture.Width, texture.Height, 10, 10, imageData.Span);
                break;
            case TextureFormat.ASTC_12x12:
            case TextureFormat.ASTC_RGBA_12x12:
            case TextureFormat.ASTC_HDR_12x12:
                AstcDecoder.DecodeASTC(textureMem.Span, texture.Width, texture.Height, 12, 12, imageData.Span);
                break;
            case TextureFormat.Unknown:
            case TextureFormat.None:
            default:
                throw new NotSupportedException($"Texture format {texture.TextureFormat} is not supported");
        }
    }
    
    private static bool UnpackCrunch(UnityVersion unityVersion, TextureFormat textureFormat, Span<byte> crunchedData, [MaybeNullWhen(false)] out byte[] data) {
        if (unityVersion >= UnityVersionRegister.Unity2017_3 || textureFormat is TextureFormat.ETC_RGB4Crunched or TextureFormat.ETC2_RGBA8Crunched) {
            data = TextureDecoder.UnpackUnityCrunch(crunchedData);
        } else {
            data = TextureDecoder.UnpackCrunch(crunchedData);
        }

        return data != null;
    }

    public static Span<byte> ToDDS(ITexture texture) {
        if (texture.ShouldDeserialize) {
            throw new IncompleteDeserialization();
        }

        return DDS.BuildDDS(texture.TextureFormat.ToD3DPixelFormat(), texture.MipCount, texture.Width, texture.Height, texture.Depth, texture.TextureData!.Value.Span);
    }

    public static void FromDDS(Texture2D texture, Stream stream, bool leaveOpen = false) {
        using var reader = new BiEndianBinaryReader(stream, leaveOpen);
        var header = reader.ReadStruct<DDSImageHeader>();

        texture.IsMutated = true;

        texture.Width = header.Width;
        texture.Height = header.Height;
        texture.MipCount = header.MipmapCount;

        switch (header.Format.FourCC) {
            case 0x30315844: { // DX10
                var dx10 = reader.ReadStruct<DXT10Header>();
                texture.TextureFormat = ((DXGIPixelFormat) dx10.Format).ToTextureFormat();
                texture.Depth = dx10.Size;
                break;
            }
            case 0x31545844: // DXT1
                texture.TextureFormat = TextureFormat.DXT1;
                break;
            case 0x34545844: // DXT4
            case 0x35545844: // DXT5
                texture.TextureFormat = TextureFormat.DXT5;
                break;
            case 0x31495441: // ATI1
                texture.TextureFormat = TextureFormat.BC4;
                break;
            case 0x32495441: // ATI2
                texture.TextureFormat = TextureFormat.BC5;
                break;
            default:
                throw new NotSupportedException($"DDS FourCC {header.Format.FourCC} is not supported");
        }

        texture.TextureData = reader.ReadMemory(reader.Unconsumed);

        if (!leaveOpen) {
            stream.Close();
        }
    }

    public static Texture2D FromDDS(UnityObjectInfo info, SerializedFile file, Stream stream, bool leaveOpen = false) {
        var texture2D = new Texture2D(info, file) { Name = "Texture2D" };

        FromDDS(texture2D, stream, leaveOpen);
        if (!leaveOpen) {
            stream.Close();
        }

        return texture2D;
    }
}
