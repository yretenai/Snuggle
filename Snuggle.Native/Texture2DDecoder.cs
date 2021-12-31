using System.Reflection;
using System.Runtime.InteropServices;

namespace Snuggle.Native;

public static unsafe partial class Texture2DDecoder {
    private const string LibraryName = "Texture2DDecoderNative";

    static Texture2DDecoder() {
        Helper.Register();
    }

    public static bool DecodeDXT1(byte[] data, int width, int height, byte[] image) {
        fixed (byte* pData = data) {
            fixed (byte* pImage = image) {
                return Native.DecodeDXT1(pData, width, height, pImage);
            }
        }
    }

    public static bool DecodeDXT5(byte[] data, int width, int height, byte[] image) {
        fixed (byte* pData = data) {
            fixed (byte* pImage = image) {
                return Native.DecodeDXT5(pData, width, height, pImage);
            }
        }
    }

    public static bool DecodePVRTC(byte[] data, int width, int height, byte[] image, bool is2bpp) {
        fixed (byte* pData = data) {
            fixed (byte* pImage = image) {
                return Native.DecodePVRTC(pData, width, height, pImage, is2bpp);
            }
        }
    }

    public static bool DecodeETC1(byte[] data, int width, int height, byte[] image) {
        fixed (byte* pData = data) {
            fixed (byte* pImage = image) {
                return Native.DecodeETC1(pData, width, height, pImage);
            }
        }
    }

    public static bool DecodeETC2(byte[] data, int width, int height, byte[] image) {
        fixed (byte* pData = data) {
            fixed (byte* pImage = image) {
                return Native.DecodeETC2(pData, width, height, pImage);
            }
        }
    }

    public static bool DecodeETC2A1(byte[] data, int width, int height, byte[] image) {
        fixed (byte* pData = data) {
            fixed (byte* pImage = image) {
                return Native.DecodeETC2A1(pData, width, height, pImage);
            }
        }
    }

    public static bool DecodeETC2A8(byte[] data, int width, int height, byte[] image) {
        fixed (byte* pData = data) {
            fixed (byte* pImage = image) {
                return Native.DecodeETC2A8(pData, width, height, pImage);
            }
        }
    }

    public static bool DecodeEACR(byte[] data, int width, int height, byte[] image) {
        fixed (byte* pData = data) {
            fixed (byte* pImage = image) {
                return Native.DecodeEACR(pData, width, height, pImage);
            }
        }
    }

    public static bool DecodeEACRSigned(byte[] data, int width, int height, byte[] image) {
        fixed (byte* pData = data) {
            fixed (byte* pImage = image) {
                return Native.DecodeEACRSigned(pData, width, height, pImage);
            }
        }
    }

    public static bool DecodeEACRG(byte[] data, int width, int height, byte[] image) {
        fixed (byte* pData = data) {
            fixed (byte* pImage = image) {
                return Native.DecodeEACRG(pData, width, height, pImage);
            }
        }
    }

    public static bool DecodeEACRGSigned(byte[] data, int width, int height, byte[] image) {
        fixed (byte* pData = data) {
            fixed (byte* pImage = image) {
                return Native.DecodeEACRGSigned(pData, width, height, pImage);
            }
        }
    }

    public static bool DecodeBC4(byte[] data, int width, int height, byte[] image) {
        fixed (byte* pData = data) {
            fixed (byte* pImage = image) {
                return Native.DecodeBC4(pData, width, height, pImage);
            }
        }
    }

    public static bool DecodeBC5(byte[] data, int width, int height, byte[] image) {
        fixed (byte* pData = data) {
            fixed (byte* pImage = image) {
                return Native.DecodeBC5(pData, width, height, pImage);
            }
        }
    }

    public static bool DecodeBC6(byte[] data, int width, int height, byte[] image) {
        fixed (byte* pData = data) {
            fixed (byte* pImage = image) {
                return Native.DecodeBC6(pData, width, height, pImage);
            }
        }
    }

    public static bool DecodeBC7(byte[] data, int width, int height, byte[] image) {
        fixed (byte* pData = data) {
            fixed (byte* pImage = image) {
                return Native.DecodeBC7(pData, width, height, pImage);
            }
        }
    }

    public static bool DecodeATCRGB4(byte[] data, int width, int height, byte[] image) {
        fixed (byte* pData = data) {
            fixed (byte* pImage = image) {
                return Native.DecodeATCRGB4(pData, width, height, pImage);
            }
        }
    }

    public static bool DecodeATCRGBA8(byte[] data, int width, int height, byte[] image) {
        fixed (byte* pData = data) {
            fixed (byte* pImage = image) {
                return Native.DecodeATCRGBA8(pData, width, height, pImage);
            }
        }
    }

    public static bool DecodeASTC(byte[] data, int width, int height, int blockWidth, int blockHeight, byte[] image) {
        fixed (byte* pData = data) {
            fixed (byte* pImage = image) {
                return Native.DecodeASTC(pData, width, height, blockWidth, blockHeight, pImage);
            }
        }
    }

    public static byte[]? UnpackCrunch(byte[] data) {
        void* pBuffer;
        uint bufferSize;

        fixed (byte* pData = data) {
            Native.UnpackCrunch(pData, (uint) data.Length, out pBuffer, out bufferSize);
        }

        if (pBuffer == null) {
            return null;
        }

        var result = new byte[bufferSize];

        Marshal.Copy(new IntPtr(pBuffer), result, 0, (int) bufferSize);

        Native.DisposeBuffer(ref pBuffer);

        return result;
    }

    public static byte[]? UnpackUnityCrunch(byte[] data) {
        void* pBuffer;
        uint bufferSize;

        fixed (byte* pData = data) {
            Native.UnpackUnityCrunch(pData, (uint) data.Length, out pBuffer, out bufferSize);
        }

        if (pBuffer == null) {
            return null;
        }

        var result = new byte[bufferSize];

        Marshal.Copy(new IntPtr(pBuffer), result, 0, (int) bufferSize);

        Native.DisposeBuffer(ref pBuffer);

        return result;
    }
}
