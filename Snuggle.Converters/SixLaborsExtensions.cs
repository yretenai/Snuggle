using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance.Buffers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;

namespace Snuggle.Converters;

public static class SixLaborsExtensions {
    public static MemoryOwner<byte> ToPixelBuffer<TPixel>(this Image<TPixel> image) where TPixel : unmanaged, IPixel<TPixel> {
        var frame = image.Frames.RootFrame;
        var group = frame.GetPixelMemoryGroup();
        var buffer = MemoryMarshal.AsBytes(group[0].Span);
        var memory = MemoryOwner<byte>.Allocate(buffer.Length);
        buffer.CopyTo(memory.Span);
        return memory;
    }
}
