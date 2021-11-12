using System;
using System.IO;
using DirectXTexNet;
using DragonLib.Imaging.DXGI;
using JetBrains.Annotations;
using Snuggle.Core;
using Snuggle.Core.Exceptions;
using Snuggle.Core.Implementations;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models.Objects.Graphics;
using Snuggle.Core.Models.Serialization;
using Texture2DDecoder;

namespace Snuggle.Converters;

[PublicAPI]
public static class Texture2DConverter {
    public static bool SupportsDDS(Texture2D texture) => texture.TextureFormat.CanSupportDDS();
    public static bool SupportsDDSConversion(Texture2D texture) => Environment.OSVersion.Platform == PlatformID.Win32NT && SupportsDDS(texture);

    public static unsafe Memory<byte> ToRGBA(Texture2D texture2D) {
        if (SupportsDDSConversion(texture2D)) {
            ScratchImage? scratch = null;
            try {
                var data = ToDDS(texture2D);
                fixed (byte* dataPin = &data.GetPinnableReference()) {
                    scratch = TexHelper.Instance.LoadFromDDSMemory((IntPtr) dataPin, data.Length, DDS_FLAGS.NONE);
                    var info = scratch.GetMetadata();

                    if (TexHelper.Instance.IsCompressed(info.Format)) {
                        var temp = scratch.Decompress(0, DXGI_FORMAT.UNKNOWN);
                        scratch.Dispose();
                        scratch = temp;
                        info = scratch.GetMetadata();
                    }

                    if (info.Format != DXGI_FORMAT.R8G8B8A8_UNORM) {
                        var temp = scratch.Convert(DXGI_FORMAT.R8G8B8A8_UNORM, TEX_FILTER_FLAGS.DEFAULT, 0.5f);
                        scratch.Dispose();
                        scratch = temp;
                    }

                    var image = scratch.GetImage(0);
                    Memory<byte> tex = new byte[image.Width * image.Height * 4];
                    Buffer.MemoryCopy((void*) image.Pixels, tex.Pin().Pointer, tex.Length, tex.Length);
                    return tex;
                }
            } finally {
                if (scratch is { IsDisposed: false }) {
                    scratch.Dispose();
                }
            }
        }

        var textureData = texture2D.TextureData!.Value.Span.ToArray();
        switch (texture2D.TextureFormat) {
            case TextureFormat.DXT1Crunched when UnpackCrunch(texture2D.SerializedFile.Version, texture2D.TextureFormat, textureData, out var data): {
                return DecodeDXT1(texture2D.Width, texture2D.Height, data);
            }
            case TextureFormat.DXT5Crunched when UnpackCrunch(texture2D.SerializedFile.Version, texture2D.TextureFormat, textureData, out var data): {
                return DecodeDXT5(texture2D.Width, texture2D.Height, data);
            }
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
            case TextureFormat.ETC_RGBA8_3DS:
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
        }

        return Memory<byte>.Empty;
    }

    private static bool UnpackCrunch(UnityVersion unityVersion, TextureFormat textureFormat, byte[] crunchedData, out byte[] data) {
        if (unityVersion >= UnityVersionRegister.Unity2017_3 || textureFormat is TextureFormat.ETC_RGB4Crunched or TextureFormat.ETC2_RGBA8Crunched) {
            data = TextureDecoder.UnpackUnityCrunch(crunchedData);
        } else {
            data = TextureDecoder.UnpackCrunch(crunchedData);
        }

        return data != null;
    }

    private static Memory<byte> DecodeDXT1(int width, int height, byte[] data) {
        var buff = new byte[width * height * 4];
        return !TextureDecoder.DecodeDXT1(data, width, height, buff) ? Memory<byte>.Empty : buff;
    }

    private static Memory<byte> DecodeDXT5(int width, int height, byte[] data) {
        var buff = new byte[width * height * 4];
        return !TextureDecoder.DecodeDXT5(data, width, height, buff) ? Memory<byte>.Empty : buff;
    }

    private static Memory<byte> DecodePVRTC(bool is2bpp, int width, int height, byte[] data) {
        var buff = new byte[width * height * 4];
        return !TextureDecoder.DecodePVRTC(data, width, height, buff, is2bpp) ? Memory<byte>.Empty : buff;
    }

    private static Memory<byte> DecodeETC1(int width, int height, byte[] data) {
        var buff = new byte[width * height * 4];
        return !TextureDecoder.DecodeETC1(data, width, height, buff) ? Memory<byte>.Empty : buff;
    }

    private static Memory<byte> DecodeATCRGB4(int width, int height, byte[] data) {
        var buff = new byte[width * height * 4];
        return !TextureDecoder.DecodeATCRGB4(data, width, height, buff) ? Memory<byte>.Empty : buff;
    }

    private static Memory<byte> DecodeATCRGBA8(int width, int height, byte[] data) {
        var buff = new byte[width * height * 4];
        return !TextureDecoder.DecodeATCRGBA8(data, width, height, buff) ? Memory<byte>.Empty : buff;
    }

    private static Memory<byte> DecodeEACR(int width, int height, byte[] data) {
        var buff = new byte[width * height * 4];
        return !TextureDecoder.DecodeEACR(data, width, height, buff) ? Memory<byte>.Empty : buff;
    }

    private static Memory<byte> DecodeEACRSigned(int width, int height, byte[] data) {
        var buff = new byte[width * height * 4];
        return !TextureDecoder.DecodeEACRSigned(data, width, height, buff) ? Memory<byte>.Empty : buff;
    }

    private static Memory<byte> DecodeEACRG(int width, int height, byte[] data) {
        var buff = new byte[width * height * 4];
        return !TextureDecoder.DecodeEACRG(data, width, height, buff) ? Memory<byte>.Empty : buff;
    }

    private static Memory<byte> DecodeEACRGSigned(int width, int height, byte[] data) {
        var buff = new byte[width * height * 4];
        return !TextureDecoder.DecodeEACRGSigned(data, width, height, buff) ? Memory<byte>.Empty : buff;
    }

    private static Memory<byte> DecodeETC2(int width, int height, byte[] data) {
        var buff = new byte[width * height * 4];
        return !TextureDecoder.DecodeETC2(data, width, height, buff) ? Memory<byte>.Empty : buff;
    }

    private static Memory<byte> DecodeETC2A1(int width, int height, byte[] data) {
        var buff = new byte[width * height * 4];
        return !TextureDecoder.DecodeETC2A1(data, width, height, buff) ? Memory<byte>.Empty : buff;
    }

    private static Memory<byte> DecodeETC2A8(int width, int height, byte[] data) {
        var buff = new byte[width * height * 4];
        return !TextureDecoder.DecodeETC2A8(data, width, height, buff) ? Memory<byte>.Empty : buff;
    }

    private static Memory<byte> DecodeASTC(int blocksize, int width, int height, byte[] data) {
        var buff = new byte[width * height * 4];
        return !TextureDecoder.DecodeASTC(data, width, height, blocksize, blocksize, buff) ? Memory<byte>.Empty : buff;
    }

    public static Span<byte> ToDDS(Texture2D texture) {
        if (texture.ShouldDeserialize) {
            throw new IncompleteDeserializationException();
        }

        return DXGI.BuildDDS(texture.TextureFormat.ToD3DPixelFormat(), texture.MipCount, texture.Width, texture.Height, texture.TextureCount, texture.TextureData!.Value.Span);
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
