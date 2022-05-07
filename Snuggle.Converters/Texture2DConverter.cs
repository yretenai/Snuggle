using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using AssetRipper.TextureDecoder;
using BCnEncoder.Decoder;
using BCnEncoder.Shared;
using Snuggle.Converters.DXGI;
using Snuggle.Core;
using Snuggle.Core.Exceptions;
using Snuggle.Core.Implementations;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models.Objects.Graphics;
using Snuggle.Core.Models.Serialization;
using Snuggle.Native;
using Half = System.Half;

namespace Snuggle.Converters;

public static partial class Texture2DConverter {
    public static bool SupportsDDS(Texture2D texture) => texture.TextureFormat.CanSupportDDS();
    public static bool UseDDSConversion(TextureFormat textureFormat) => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && textureFormat.CanSupportDDS();

    public static Memory<byte> ToRGBA(Texture2D texture2D, bool useDirectXTex, bool useTextureDecoder) {
        if (texture2D.TextureData!.Value.IsEmpty) {
            return Memory<byte>.Empty;
        }

        if (useDirectXTex && UseDDSConversion(texture2D.TextureFormat)) {
            var data = ToRGBADirectX(texture2D);
            if (!data.IsEmpty) {
                return data;
            }
        }

        if (useTextureDecoder) {
            var textureData = texture2D.TextureData.Value.Span.ToArray();
            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (texture2D.TextureFormat) {
                case TextureFormat.DXT1Crunched when UnpackCrunch(texture2D.SerializedFile.Version, texture2D.TextureFormat, textureData, out var data): {
                    return DecodeDXT1(texture2D.Width, texture2D.Height, data);
                }
                case TextureFormat.DXT5Crunched when UnpackCrunch(texture2D.SerializedFile.Version, texture2D.TextureFormat, textureData, out var data): {
                    return DecodeDXT5(texture2D.Width, texture2D.Height, data);
                }
                case TextureFormat.DXT1:
                    return DecodeDXT1(texture2D.Width, texture2D.Height, textureData);
                case TextureFormat.DXT5:
                    return DecodeDXT5(texture2D.Width, texture2D.Height, textureData);
                case TextureFormat.PVRTC_RGB2:
                case TextureFormat.PVRTC_RGBA2:
                    return DecodePVRTC(true, texture2D.Width, texture2D.Height, textureData);
                case TextureFormat.PVRTC_RGB4:
                case TextureFormat.PVRTC_RGBA4:
                    return DecodePVRTC(false, texture2D.Width, texture2D.Height, textureData);
                case TextureFormat.ETC_RGB4:
                case TextureFormat.ETC_RGB4_3DS:
                    return DecodeETC1(texture2D.Width, texture2D.Height, textureData);
                case TextureFormat.ATC_RGB4:
                    return DecodeATCRGB4(texture2D.Width, texture2D.Height, textureData);
                case TextureFormat.ATC_RGBA8:
                    return DecodeATCRGBA8(texture2D.Width, texture2D.Height, textureData);
                case TextureFormat.EAC_R:
                    return DecodeEACR(texture2D.Width, texture2D.Height, textureData);
                case TextureFormat.EAC_R_SIGNED:
                    return DecodeEACRSigned(texture2D.Width, texture2D.Height, textureData);
                case TextureFormat.EAC_RG:
                    return DecodeEACRG(texture2D.Width, texture2D.Height, textureData);
                case TextureFormat.EAC_RG_SIGNED:
                    return DecodeEACRGSigned(texture2D.Width, texture2D.Height, textureData);
                case TextureFormat.ETC2_RGB:
                    return DecodeETC2(texture2D.Width, texture2D.Height, textureData);
                case TextureFormat.ETC2_RGBA1:
                    return DecodeETC2A1(texture2D.Width, texture2D.Height, textureData);
                case TextureFormat.ETC2_RGBA8:
                case TextureFormat.ETC2_RGBA8_3DS:
                    return DecodeETC2A8(texture2D.Width, texture2D.Height, textureData);
                case TextureFormat.ASTC_4x4:
                case TextureFormat.ASTC_ALPHA_4x4:
                case TextureFormat.ASTC_HDR_4x4:
                    return DecodeASTC(4, texture2D.Width, texture2D.Height, textureData);
                case TextureFormat.ASTC_5x5:
                case TextureFormat.ASTC_ALPHA_5x5:
                case TextureFormat.ASTC_HDR_5x5:
                    return DecodeASTC(5, texture2D.Width, texture2D.Height, textureData);
                case TextureFormat.ASTC_6x6:
                case TextureFormat.ASTC_ALPHA_6x6:
                case TextureFormat.ASTC_HDR_6x6:
                    return DecodeASTC(6, texture2D.Width, texture2D.Height, textureData);
                case TextureFormat.ASTC_8x8:
                case TextureFormat.ASTC_ALPHA_8x8:
                case TextureFormat.ASTC_HDR_8x8:
                    return DecodeASTC(8, texture2D.Width, texture2D.Height, textureData);
                case TextureFormat.ASTC_10x10:
                case TextureFormat.ASTC_ALPHA_10x10:
                case TextureFormat.ASTC_HDR_10x10:
                    return DecodeASTC(10, texture2D.Width, texture2D.Height, textureData);
                case TextureFormat.ASTC_12x12:
                case TextureFormat.ASTC_ALPHA_12x12:
                case TextureFormat.ASTC_HDR_12x12:
                    return DecodeASTC(12, texture2D.Width, texture2D.Height, textureData);
                case TextureFormat.ETC_RGB4Crunched when UnpackCrunch(texture2D.SerializedFile.Version, texture2D.TextureFormat, textureData, out var data): {
                    return DecodeETC1(texture2D.Width, texture2D.Height, data);
                }
                case TextureFormat.ETC2_RGBA8Crunched when UnpackCrunch(texture2D.SerializedFile.Version, texture2D.TextureFormat, textureData, out var data): {
                    return DecodeETC2A8(texture2D.Width, texture2D.Height, data);
                }
                case TextureFormat.Alpha8:
                case TextureFormat.R8:
                    return DecodeA8(texture2D.Width, texture2D.Height, textureData);
                case TextureFormat.ARGB4444:
                    return DecodeARGB4444(texture2D.Width, texture2D.Height, textureData);
                case TextureFormat.RGB24:
                    return DecodeRGB24(texture2D.Width, texture2D.Height, textureData);
                case TextureFormat.RGBA32:
                case TextureFormat.BGRA32:
                case TextureFormat.ARGB32:
                    return textureData;
                case TextureFormat.RGB565:
                    return DecodeRGB565(texture2D.Width, texture2D.Height, textureData);
                case TextureFormat.R16:
                    return DecodeR16(texture2D.Width, texture2D.Height, textureData);
                case TextureFormat.RG16:
                    return DecodeRG16(texture2D.Width, texture2D.Height, textureData);
                case TextureFormat.RGBA4444:
                    return DecodeRGBA4444(texture2D.Width, texture2D.Height, textureData);
                case TextureFormat.RHalf:
                    return DecodeR16H(texture2D.Width, texture2D.Height, textureData);
                case TextureFormat.RGHalf:
                    return DecodeRG16H(texture2D.Width, texture2D.Height, textureData);
                case TextureFormat.RGBAHalf:
                    return DecodeRGBA16H(texture2D.Width, texture2D.Height, textureData);
                case TextureFormat.RFloat:
                    return DecodeRF(texture2D.Width, texture2D.Height, textureData);
                case TextureFormat.RGFloat:
                    return DecodeRGF(texture2D.Width, texture2D.Height, textureData);
                case TextureFormat.RGBAFloat:
                    return DecodeRGBAF(texture2D.Width, texture2D.Height, textureData);
                case TextureFormat.YUY2:
                    return DecodeYUY2(texture2D.Width, texture2D.Height, textureData);
                case TextureFormat.RGB9e5Float:
                    return DecodeRGB9E5(texture2D.Width, texture2D.Height, textureData);
                case TextureFormat.BC4:
                    return DecodeBC4(texture2D.Width, texture2D.Height, textureData);
                case TextureFormat.BC5:
                    return DecodeBC5(texture2D.Width, texture2D.Height, textureData);
                case TextureFormat.BC6H:
                    return DecodeBC6H(texture2D.Width, texture2D.Height, textureData);
                case TextureFormat.BC7:
                    return DecodeBC7(texture2D.Width, texture2D.Height, textureData);
            }
        }

        var textureMem = texture2D.TextureData.Value;
        var imageData = new byte[texture2D.Width * texture2D.Height * 4].AsMemory();

        switch (texture2D.TextureFormat) {
            case TextureFormat.Alpha8:
                RgbDecoder.A8ToBGRA32(textureMem.Span, texture2D.Width, texture2D.Height, imageData.Span);
                break;
            case TextureFormat.ARGB4444:
                RgbDecoder.ARGB4444ToBGRA32(textureMem.Span, texture2D.Width, texture2D.Height, imageData.Span);
                break;
            case TextureFormat.RGB24:
                RgbDecoder.RGB24ToBGRA32(textureMem.Span, texture2D.Width, texture2D.Height, imageData.Span);
                break;
            case TextureFormat.RGBA32:
                RgbDecoder.RGBA32ToBGRA32(textureMem.Span, texture2D.Width, texture2D.Height, imageData.Span);
                break;
            case TextureFormat.ARGB32:
                RgbDecoder.ARGB32ToBGRA32(textureMem.Span, texture2D.Width, texture2D.Height, imageData.Span);
                break;
            case TextureFormat.RGB565:
                RgbDecoder.RGB565ToBGRA32(textureMem.Span, texture2D.Width, texture2D.Height, imageData.Span);
                break;
            case TextureFormat.R16:
                RgbDecoder.R16ToBGRA32(textureMem.Span, texture2D.Width, texture2D.Height, imageData.Span);
                break;
            case TextureFormat.RGBA4444:
                RgbDecoder.RGBA16ToBGRA32(textureMem.Span, texture2D.Width, texture2D.Height, imageData.Span);
                break;
            case TextureFormat.RHalf:
                RgbDecoder.R16FToBGRA32(textureMem.Span, texture2D.Width, texture2D.Height, imageData.Span);
                break;
            case TextureFormat.RGHalf:
                RgbDecoder.RG16FToBGRA32(textureMem.Span, texture2D.Width, texture2D.Height, imageData.Span);
                break;
            case TextureFormat.RGBAHalf:
                RgbDecoder.RGBA16FToBGRA32(textureMem.Span, texture2D.Width, texture2D.Height, imageData.Span);
                break;
            case TextureFormat.RFloat:
                RgbDecoder.R32FToBGRA32(textureMem.Span, texture2D.Width, texture2D.Height, imageData.Span);
                break;
            case TextureFormat.RGFloat:
                RgbDecoder.RG32FToBGRA32(textureMem.Span, texture2D.Width, texture2D.Height, imageData.Span);
                break;
            case TextureFormat.RGBAFloat:
                RgbDecoder.RGBA32FToBGRA32(textureMem.Span, texture2D.Width, texture2D.Height, imageData.Span);
                break;
            case TextureFormat.RGB9e5Float:
                RgbDecoder.RGB9e5FToBGRA32(textureMem.Span, texture2D.Width, texture2D.Height, imageData.Span);
                break;
            case TextureFormat.RG16:
                RgbDecoder.RG16ToBGRA32(textureMem.Span, texture2D.Width, texture2D.Height, imageData.Span);
                break;
            case TextureFormat.R8:
                RgbDecoder.R8ToBGRA32(textureMem.Span, texture2D.Width, texture2D.Height, imageData.Span);
                break;
            case TextureFormat.YUY2:
                Yuy2Decoder.DecompressYUY2(textureMem.Span, texture2D.Width, texture2D.Height, imageData.Span);
                break;
            case TextureFormat.DXT1: {
                var bc = new BcDecoder();
                var pixels = bc.DecodeRaw(textureMem.ToArray(), texture2D.Width, texture2D.Height, CompressionFormat.Bc1WithAlpha);
                if (pixels != null) {
                    MemoryMarshal.AsBytes(pixels.AsSpan()).CopyTo(imageData.Span);
                }
                break;
            }
            case TextureFormat.DXT1Crunched when UnpackCrunch(texture2D.SerializedFile.Version, texture2D.TextureFormat, textureMem.ToArray(), out var data): {
                var bc = new BcDecoder();
                var pixels = bc.DecodeRaw(data, texture2D.Width, texture2D.Height, CompressionFormat.Bc1);
                if (pixels != null) {
                    MemoryMarshal.AsBytes(pixels.AsSpan()).CopyTo(imageData.Span);
                }
                break;
            }
            case TextureFormat.DXT5:{
                var bc = new BcDecoder();
                var pixels =bc.DecodeRaw(textureMem.ToArray(), texture2D.Width, texture2D.Height, CompressionFormat.Bc3);
                if (pixels != null) {
                    MemoryMarshal.AsBytes(pixels.AsSpan()).CopyTo(imageData.Span);
                }
                break;
            }
            case TextureFormat.DXT5Crunched when UnpackCrunch(texture2D.SerializedFile.Version, texture2D.TextureFormat, textureMem.ToArray(), out var data): {
                var bc = new BcDecoder();
                var pixels = bc.DecodeRaw(data, texture2D.Width, texture2D.Height, CompressionFormat.Bc3);
                if (pixels != null) {
                    MemoryMarshal.AsBytes(pixels.AsSpan()).CopyTo(imageData.Span);
                }
                break;
            }
            case TextureFormat.BC4: {
                var bc = new BcDecoder();
                var pixels =bc.DecodeRaw(textureMem.ToArray(), texture2D.Width, texture2D.Height, CompressionFormat.Bc4);
                if (pixels != null) {
                    MemoryMarshal.AsBytes(pixels.AsSpan()).CopyTo(imageData.Span);
                }
                break;
            }
            case TextureFormat.BC5: {
                var bc = new BcDecoder();
                var pixels =bc.DecodeRaw(textureMem.ToArray(), texture2D.Width, texture2D.Height, CompressionFormat.Bc5);
                if (pixels != null) {
                    MemoryMarshal.AsBytes(pixels.AsSpan()).CopyTo(imageData.Span);
                }
                break;
            }
            case TextureFormat.BC6H: {
                var bc = new BcDecoder();
                var pixels = bc.DecodeRaw(textureMem.ToArray(), texture2D.Width, texture2D.Height, CompressionFormat.Bc6S);
                if (pixels != null) {
                    MemoryMarshal.AsBytes(pixels.AsSpan()).CopyTo(imageData.Span);
                }
                break;
            }
            case TextureFormat.BC7:{
                var bc = new BcDecoder();
                var pixels = bc.DecodeRaw(textureMem.ToArray(), texture2D.Width, texture2D.Height, CompressionFormat.Bc7);
                if (pixels != null) {
                    MemoryMarshal.AsBytes(pixels.AsSpan()).CopyTo(imageData.Span);
                }
                break;
            }
            case TextureFormat.PVRTC_RGB2:
            case TextureFormat.PVRTC_RGBA2:
                PvrtcDecoder.DecompressPVRTC(textureMem.Span, texture2D.Width, texture2D.Height, imageData.Span, true);
                break;
            case TextureFormat.PVRTC_RGB4:
            case TextureFormat.PVRTC_RGBA4:
                PvrtcDecoder.DecompressPVRTC(textureMem.Span, texture2D.Width, texture2D.Height, imageData.Span, false);
                break;
            case TextureFormat.ATC_RGB4:
                AtcDecoder.DecompressAtcRgb4(textureMem.Span, texture2D.Width, texture2D.Height, imageData.Span);
                break;
            case TextureFormat.ATC_RGBA8:
                AtcDecoder.DecompressAtcRgba8(textureMem.Span, texture2D.Width, texture2D.Height, imageData.Span);
                break;
            case TextureFormat.ETC_RGB4:
            case TextureFormat.ETC_RGB4_3DS:
                EtcDecoder.DecompressETC(textureMem.Span, texture2D.Width, texture2D.Height, imageData.Span);
                break;
            case TextureFormat.ETC_RGB4Crunched when UnpackCrunch(texture2D.SerializedFile.Version, texture2D.TextureFormat, textureMem.ToArray(), out var data): {
                EtcDecoder.DecompressETC(data, texture2D.Width, texture2D.Height, imageData.Span);
                break;
            }
            case TextureFormat.EAC_R:
                EtcDecoder.DecompressEACRUnsigned(textureMem.Span, texture2D.Width, texture2D.Height, imageData.Span);
                break;
            case TextureFormat.EAC_R_SIGNED:
                EtcDecoder.DecompressEACRSigned(textureMem.Span, texture2D.Width, texture2D.Height, imageData.Span);
                break;
            case TextureFormat.EAC_RG:
                EtcDecoder.DecompressEACRGUnsigned(textureMem.Span, texture2D.Width, texture2D.Height, imageData.Span);
                break;
            case TextureFormat.EAC_RG_SIGNED:
                EtcDecoder.DecompressEACRGSigned(textureMem.Span, texture2D.Width, texture2D.Height, imageData.Span);
                break;
            case TextureFormat.ETC2_RGB:
                EtcDecoder.DecompressETC2(textureMem.Span, texture2D.Width, texture2D.Height, imageData.Span);
                break;
            case TextureFormat.ETC2_RGBA1:
                EtcDecoder.DecompressETC2A1(textureMem.Span, texture2D.Width, texture2D.Height, imageData.Span);
                break;
            case TextureFormat.ETC2_RGBA8:
            case TextureFormat.ETC2_RGBA8_3DS:
                EtcDecoder.DecompressETC2A8(textureMem.Span, texture2D.Width, texture2D.Height, imageData.Span);
                break;
            case TextureFormat.ETC2_RGBA8Crunched when UnpackCrunch(texture2D.SerializedFile.Version, texture2D.TextureFormat, textureMem.ToArray(), out var data): {
                EtcDecoder.DecompressETC2A8(data, texture2D.Width, texture2D.Height, imageData.Span);
                break;
            }
            case TextureFormat.ASTC_4x4:
            case TextureFormat.ASTC_ALPHA_4x4:
            case TextureFormat.ASTC_HDR_4x4:
                AstcDecoder.DecodeASTC(textureMem.Span, texture2D.Width, texture2D.Height, 4, 4,  imageData.Span);
                break;
            case TextureFormat.ASTC_5x5:
            case TextureFormat.ASTC_ALPHA_5x5:
            case TextureFormat.ASTC_HDR_5x5:
                AstcDecoder.DecodeASTC(textureMem.Span, texture2D.Width, texture2D.Height, 5, 5,  imageData.Span);
                break;
            case TextureFormat.ASTC_6x6:
            case TextureFormat.ASTC_ALPHA_6x6:
            case TextureFormat.ASTC_HDR_6x6:
                AstcDecoder.DecodeASTC(textureMem.Span, texture2D.Width, texture2D.Height, 6, 6,  imageData.Span);
                break;
            case TextureFormat.ASTC_8x8:
            case TextureFormat.ASTC_ALPHA_8x8:
            case TextureFormat.ASTC_HDR_8x8:
                AstcDecoder.DecodeASTC(textureMem.Span, texture2D.Width, texture2D.Height, 8, 8,  imageData.Span);
                break;
            case TextureFormat.ASTC_10x10:
            case TextureFormat.ASTC_ALPHA_10x10:
            case TextureFormat.ASTC_HDR_10x10:
                AstcDecoder.DecodeASTC(textureMem.Span, texture2D.Width, texture2D.Height, 10, 10,  imageData.Span);
                break;
            case TextureFormat.ASTC_12x12:
            case TextureFormat.ASTC_ALPHA_12x12:
            case TextureFormat.ASTC_HDR_12x12:
                AstcDecoder.DecodeASTC(textureMem.Span, texture2D.Width, texture2D.Height, 12, 12,  imageData.Span);
                break;
        }

        return imageData;

    }

    private static bool UnpackCrunch(UnityVersion unityVersion, TextureFormat textureFormat, byte[] crunchedData, [MaybeNullWhen(false)] out byte[] data) {
        if (unityVersion >= UnityVersionRegister.Unity2017_3 || textureFormat is TextureFormat.ETC_RGB4Crunched or TextureFormat.ETC2_RGBA8Crunched) {
            data = Texture2DDecoder.UnpackUnityCrunch(crunchedData);
        } else {
            data = Texture2DDecoder.UnpackCrunch(crunchedData);
        }

        return data != null;
    }

    private static Memory<byte> DecodeDXT1(int width, int height, byte[] data) {
        var buff = new byte[width * height * 4];
        return !Texture2DDecoder.DecodeDXT1(data, width, height, buff) ? Memory<byte>.Empty : buff;
    }

    private static Memory<byte> DecodeDXT5(int width, int height, byte[] data) {
        var buff = new byte[width * height * 4];
        return !Texture2DDecoder.DecodeDXT5(data, width, height, buff) ? Memory<byte>.Empty : buff;
    }

    private static Memory<byte> DecodeBC4(int width, int height, byte[] data) {
        var buff = new byte[width * height * 4];
        return !Texture2DDecoder.DecodeBC4(data, width, height, buff) ? Memory<byte>.Empty : buff;
    }

    private static Memory<byte> DecodeBC5(int width, int height, byte[] data) {
        var buff = new byte[width * height * 4];
        return !Texture2DDecoder.DecodeBC5(data, width, height, buff) ? Memory<byte>.Empty : buff;
    }

    private static Memory<byte> DecodeBC6H(int width, int height, byte[] data) {
        var buff = new byte[width * height * 4];
        return !Texture2DDecoder.DecodeBC6(data, width, height, buff) ? Memory<byte>.Empty : buff;
    }

    private static Memory<byte> DecodeBC7(int width, int height, byte[] data) {
        var buff = new byte[width * height * 4];
        return !Texture2DDecoder.DecodeBC7(data, width, height, buff) ? Memory<byte>.Empty : buff;
    }

    private static Memory<byte> DecodePVRTC(bool is2bpp, int width, int height, byte[] data) {
        var buff = new byte[width * height * 4];
        return !Texture2DDecoder.DecodePVRTC(data, width, height, buff, is2bpp) ? Memory<byte>.Empty : buff;
    }

    private static Memory<byte> DecodeETC1(int width, int height, byte[] data) {
        var buff = new byte[width * height * 4];
        return !Texture2DDecoder.DecodeETC1(data, width, height, buff) ? Memory<byte>.Empty : buff;
    }

    private static Memory<byte> DecodeATCRGB4(int width, int height, byte[] data) {
        var buff = new byte[width * height * 4];
        return !Texture2DDecoder.DecodeATCRGB4(data, width, height, buff) ? Memory<byte>.Empty : buff;
    }

    private static Memory<byte> DecodeATCRGBA8(int width, int height, byte[] data) {
        var buff = new byte[width * height * 4];
        return !Texture2DDecoder.DecodeATCRGBA8(data, width, height, buff) ? Memory<byte>.Empty : buff;
    }

    private static Memory<byte> DecodeEACR(int width, int height, byte[] data) {
        var buff = new byte[width * height * 4];
        return !Texture2DDecoder.DecodeEACR(data, width, height, buff) ? Memory<byte>.Empty : buff;
    }

    private static Memory<byte> DecodeEACRSigned(int width, int height, byte[] data) {
        var buff = new byte[width * height * 4];
        return !Texture2DDecoder.DecodeEACRSigned(data, width, height, buff) ? Memory<byte>.Empty : buff;
    }

    private static Memory<byte> DecodeEACRG(int width, int height, byte[] data) {
        var buff = new byte[width * height * 4];
        return !Texture2DDecoder.DecodeEACRG(data, width, height, buff) ? Memory<byte>.Empty : buff;
    }

    private static Memory<byte> DecodeEACRGSigned(int width, int height, byte[] data) {
        var buff = new byte[width * height * 4];
        return !Texture2DDecoder.DecodeEACRGSigned(data, width, height, buff) ? Memory<byte>.Empty : buff;
    }

    private static Memory<byte> DecodeETC2(int width, int height, byte[] data) {
        var buff = new byte[width * height * 4];
        return !Texture2DDecoder.DecodeETC2(data, width, height, buff) ? Memory<byte>.Empty : buff;
    }

    private static Memory<byte> DecodeETC2A1(int width, int height, byte[] data) {
        var buff = new byte[width * height * 4];
        return !Texture2DDecoder.DecodeETC2A1(data, width, height, buff) ? Memory<byte>.Empty : buff;
    }

    private static Memory<byte> DecodeETC2A8(int width, int height, byte[] data) {
        var buff = new byte[width * height * 4];
        return !Texture2DDecoder.DecodeETC2A8(data, width, height, buff) ? Memory<byte>.Empty : buff;
    }

    private static Memory<byte> DecodeASTC(int blocksize, int width, int height, byte[] data) {
        var buff = new byte[width * height * 4];
        return !Texture2DDecoder.DecodeASTC(data, width, height, blocksize, blocksize, buff) ? Memory<byte>.Empty : buff;
    }

    private static Memory<byte> DecodeA8(int width, int height, IReadOnlyList<byte> data) {
        var memory = new Memory<byte>(new byte[width * height * 4]);
        for (var i = 0; i < data.Count; ++i) {
            memory.Span[i * 4] = data[i];
            memory.Span[i * 4 + 3] = 0xFF;
        }

        return memory;
    }

    private static Memory<byte> DecodeARGB4444(int width, int height, Span<byte> data) {
        var memory = new Memory<byte>(new byte[width * height * 4]);
        for (var i = 0; i < width * height; ++i) {
            var value = MemoryMarshal.Read<ushort>(data[(i * 2)..]);
            memory.Span[i * 4 + 0] = (byte) (((value & 0x00f0) >> 4) * 0x11);
            memory.Span[i * 4 + 1] = (byte) (((value & 0x0f00) >> 8) * 0x11);
            memory.Span[i * 4 + 2] = (byte) (((value & 0xf000) >> 12) * 0x11);
            memory.Span[i * 4 + 3] = (byte) ((value & 0x000f) * 0x11);
        }

        return memory;
    }

    private static Memory<byte> DecodeRGBA4444(int width, int height, Span<byte> data) {
        var memory = new Memory<byte>(new byte[width * height * 4]);
        for (var i = 0; i < width * height; ++i) {
            var value = MemoryMarshal.Read<ushort>(data[(i * 2)..]);
            memory.Span[i * 4 + 0] = (byte) ((value & 0x000f) * 0x11);
            memory.Span[i * 4 + 1] = (byte) (((value & 0x00f0) >> 4) * 0x11);
            memory.Span[i * 4 + 2] = (byte) (((value & 0x0f00) >> 8) * 0x11);
            memory.Span[i * 4 + 3] = (byte) (((value & 0xf000) >> 12) * 0x11);
        }

        return memory;
    }

    private static Memory<byte> DecodeRGB24(int width, int height, IReadOnlyList<byte> data) {
        var memory = new Memory<byte>(new byte[width * height * 4]);
        for (var i = 0; i < width * height; ++i) {
            memory.Span[i * 4 + 0] = data[i * 3 + 0];
            memory.Span[i * 4 + 1] = data[i * 3 + 1];
            memory.Span[i * 4 + 2] = data[i * 3 + 2];
            memory.Span[i * 4 + 3] = 0xFF;
        }

        return memory;
    }

    private static Memory<byte> DecodeRGB565(int width, int height, Span<byte> data) {
        var memory = new Memory<byte>(new byte[width * height * 4]);
        for (var i = 0; i < width * height; ++i) {
            var value = MemoryMarshal.Read<ushort>(data[(i * 2)..]);
            memory.Span[i * 4 + 0] = (byte) ((value << 3) | ((value >> 2) & 7));
            memory.Span[i * 4 + 1] = (byte) (((value >> 3) & 0xfc) | ((value >> 9) & 3));
            memory.Span[i * 4 + 2] = (byte) (((value >> 8) & 0xf8) | (value >> 13));
            memory.Span[i * 4 + 3] = 0xFF;
        }

        return memory;
    }

    private static Memory<byte> DecodeR16(int width, int height, Span<byte> data) {
        var memory = new Memory<byte>(new byte[width * height * 4]);
        for (var i = 0; i < width * height; ++i) {
            memory.Span[i * 4 + 0] = (byte) (MemoryMarshal.Read<ushort>(data[(i * 2)..]) / 2);
            memory.Span[i * 4 + 3] = 0xFF;
        }

        return memory;
    }

    private static Memory<byte> DecodeRG16(int width, int height, Span<byte> data) {
        var memory = new Memory<byte>(new byte[width * height * 4]);
        for (var i = 0; i < width * height; ++i) {
            memory.Span[i * 4 + 0] = (byte) (MemoryMarshal.Read<ushort>(data[(i * 4)..]) / 2);
            memory.Span[i * 4 + 1] = (byte) (MemoryMarshal.Read<ushort>(data[(i * 4 + 2)..]) / 2);
            memory.Span[i * 4 + 3] = 0xFF;
        }

        return memory;
    }

    private static Memory<byte> DecodeR16H(int width, int height, Span<byte> data) {
        var memory = new Memory<byte>(new byte[width * height * 4]);
        for (var i = 0; i < width * height; ++i) {
            memory.Span[i * 4 + 0] = (byte) ((float) MemoryMarshal.Read<Half>(data[(i * 2)..]) * 0xff);
            memory.Span[i * 4 + 3] = 0xFF;
        }

        return memory;
    }

    private static Memory<byte> DecodeRG16H(int width, int height, Span<byte> data) {
        var memory = new Memory<byte>(new byte[width * height * 4]);
        for (var i = 0; i < width * height; ++i) {
            memory.Span[i * 4 + 0] = (byte) ((float) MemoryMarshal.Read<Half>(data[(i * 4)..]) * 0xff);
            memory.Span[i * 4 + 1] = (byte) ((float) MemoryMarshal.Read<Half>(data[(i * 4 + 2)..]) * 0xff);
            memory.Span[i * 4 + 3] = 0xFF;
        }

        return memory;
    }

    private static Memory<byte> DecodeRGBA16H(int width, int height, Span<byte> data) {
        var memory = new Memory<byte>(new byte[width * height * 4]);
        for (var i = 0; i < width * height; ++i) {
            memory.Span[i * 4 + 0] = (byte) ((float) MemoryMarshal.Read<Half>(data[(i * 8)..]) * 0xff);
            memory.Span[i * 4 + 1] = (byte) ((float) MemoryMarshal.Read<Half>(data[(i * 8 + 2)..]) * 0xff);
            memory.Span[i * 4 + 2] = (byte) ((float) MemoryMarshal.Read<Half>(data[(i * 8 + 4)..]) * 0xff);
            memory.Span[i * 4 + 3] = (byte) ((float) MemoryMarshal.Read<Half>(data[(i * 8 + 6)..]) * 0xff);
        }

        return memory;
    }

    private static Memory<byte> DecodeRF(int width, int height, Span<byte> data) {
        var memory = new Memory<byte>(new byte[width * height * 4]);
        for (var i = 0; i < width * height; ++i) {
            memory.Span[i * 4 + 0] = (byte) (MemoryMarshal.Read<float>(data[(i * 4)..]) * 0xff);
            memory.Span[i * 4 + 3] = 0xFF;
        }

        return memory;
    }

    private static Memory<byte> DecodeRGF(int width, int height, Span<byte> data) {
        var memory = new Memory<byte>(new byte[width * height * 4]);
        for (var i = 0; i < width * height; ++i) {
            memory.Span[i * 4 + 0] = (byte) (MemoryMarshal.Read<float>(data[(i * 8)..]) * 0xff);
            memory.Span[i * 4 + 1] = (byte) (MemoryMarshal.Read<float>(data[(i * 8 + 4)..]) * 0xff);
            memory.Span[i * 4 + 3] = 0xFF;
        }

        return memory;
    }

    private static Memory<byte> DecodeRGBAF(int width, int height, Span<byte> data) {
        var memory = new Memory<byte>(new byte[width * height * 4]);
        for (var i = 0; i < width * height; ++i) {
            memory.Span[i * 4 + 0] = (byte) (MemoryMarshal.Read<float>(data[(i * 16)..]) * 0xff);
            memory.Span[i * 4 + 1] = (byte) (MemoryMarshal.Read<float>(data[(i * 16 + 4)..]) * 0xff);
            memory.Span[i * 4 + 2] = (byte) (MemoryMarshal.Read<float>(data[(i * 16 + 8)..]) * 0xff);
            memory.Span[i * 4 + 3] = (byte) (MemoryMarshal.Read<float>(data[(i * 16 + 12)..]) * 0xff);
        }

        return memory;
    }

    private static byte ClampByte(int x) => (byte) (byte.MaxValue < x ? byte.MaxValue : x > byte.MinValue ? x : byte.MinValue);

    private static Memory<byte> DecodeYUY2(int width, int height, IReadOnlyList<byte> data) {
        var memory = new Memory<byte>(new byte[width * height * 4]);
        var p = 0;
        var o = 0;
        var halfWidth = width / 2;
        for (var j = 0; j < height; j++) {
            for (var i = 0; i < halfWidth; ++i) {
                int y0 = data[p++];
                int u0 = data[p++];
                int y1 = data[p++];
                int v0 = data[p++];
                var c = y0 - 16;
                var d = u0 - 128;
                var e = v0 - 128;
                memory.Span[o++] = ClampByte((298 * c + 516 * d + 128) >> 8); // b
                memory.Span[o++] = ClampByte((298 * c - 100 * d - 208 * e + 128) >> 8); // g
                memory.Span[o++] = ClampByte((298 * c + 409 * e + 128) >> 8); // r
                memory.Span[o++] = 255;
                c = y1 - 16;
                memory.Span[o++] = ClampByte((298 * c + 516 * d + 128) >> 8); // b
                memory.Span[o++] = ClampByte((298 * c - 100 * d - 208 * e + 128) >> 8); // g
                memory.Span[o++] = ClampByte((298 * c + 409 * e + 128) >> 8); // r
                memory.Span[o++] = 255;
            }
        }

        return memory;
    }

    private static Memory<byte> DecodeRGB9E5(int width, int height, Span<byte> data) {
        var memory = new Memory<byte>(new byte[width * height * 4]);
        for (var i = 0; i < data.Length; i += 4) {
            var n = MemoryMarshal.Read<int>(data[i..]);
            var scale = (n >> 27) & 0x1f;
            var scalef = Math.Pow(2, scale - 24);
            var b = (n >> 18) & 0x1ff;
            var g = (n >> 9) & 0x1ff;
            var r = n & 0x1ff;
            memory.Span[i] = (byte) Math.Round(r * scalef * 0xff);
            memory.Span[i + 1] = (byte) Math.Round(g * scalef * 0xff);
            memory.Span[i + 2] = (byte) Math.Round(b * scalef * 0xff);
            memory.Span[i + 3] = 255;
        }

        return memory;
    }

    public static Span<byte> ToDDS(Texture2D texture) {
        if (texture.ShouldDeserialize) {
            throw new IncompleteDeserialization();
        }

        return DDS.BuildDDS(texture.TextureFormat.ToD3DPixelFormat(), texture.MipCount, texture.Width, texture.Height, texture.TextureCount, texture.TextureData!.Value.Span);
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
                texture.TextureCount = dx10.Size;
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
