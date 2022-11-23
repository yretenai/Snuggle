using System;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;

namespace Snuggle.Converters;

public static class SixLaborsExtensions {
    public static Memory<byte> ToPixelBuffer<TPixel>(this Image<TPixel> image) where TPixel : unmanaged, IPixel<TPixel> {
        var frame = image.Frames.RootFrame;
        var group = frame.GetPixelMemoryGroup();
        var buffer = MemoryMarshal.AsBytes(group[0].Span);
        return new Memory<byte>(buffer.ToArray());
    }
}
