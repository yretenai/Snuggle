using System;
using System.Runtime.InteropServices;
using DirectXTexNet;
using Snuggle.Core.Implementations;

namespace Snuggle.Converters;

public static partial class Texture2DConverter {
    public static unsafe Memory<byte> ToRGBADirectX(Texture2D texture2D) {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            return Memory<byte>.Empty;
        }

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
}
