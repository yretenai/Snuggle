using System;
using System.Runtime.InteropServices;
using DirectXTexNet;
using Snuggle.Core.Interfaces;

namespace Snuggle.Converters;

public static partial class Texture2DConverter {
    public static unsafe Memory<byte> ToRGBADirectX(ITexture texture2D) {
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
                    var temp = scratch.Decompress(DXGI_FORMAT.UNKNOWN);
                    scratch.Dispose();
                    scratch = temp;
                    info = scratch.GetMetadata();
                }

                if (info.Format != DXGI_FORMAT.R8G8B8A8_UNORM) {
                    var temp = scratch.Convert(DXGI_FORMAT.R8G8B8A8_UNORM, TEX_FILTER_FLAGS.DEFAULT, 0.5f);
                    scratch.Dispose();
                    scratch = temp;
                }

                info = scratch.GetMetadata();

                Memory<byte> tex = new byte[texture2D.Width * texture2D.Height * texture2D.Depth * 4];
                using var pin = tex.Pin();
                var offset = 0;
                for (var i = 0; i < info.ArraySize; ++i) {
                    var image = scratch.GetImage(i);
                    Buffer.MemoryCopy((void*) image.Pixels, (void*) ((nint) pin.Pointer + offset), tex.Length - offset, image.SlicePitch);
                    offset += (int) image.SlicePitch;
                }

                return tex;
            }
        } finally {
            if (scratch is { IsDisposed: false }) {
                scratch.Dispose();
            }
        }
    }
}
