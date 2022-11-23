using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Snuggle;

public class BGRABitmapSource : BitmapSource {
    private readonly int BackingPixelHeight;
    private readonly int BackingPixelWidth;
    public readonly int Frames;

    public BGRABitmapSource(Memory<byte> buffer, int pixelWidth, int pixelHeight, int frames) {
        Buffer = buffer;
        BackingPixelWidth = pixelWidth;
        BackingPixelHeight = pixelHeight;
        Frames = frames;
    }

    public BGRABitmapSource(BGRABitmapSource bitmap) {
        Buffer = bitmap.Buffer;
        BackingPixelWidth = bitmap.BackingPixelWidth;
        BackingPixelHeight = bitmap.BackingPixelHeight;
        HideRed = bitmap.HideRed;
        HideGreen = bitmap.HideGreen;
        HideBlue = bitmap.HideBlue;
        HideAlpha = bitmap.HideAlpha;
        Frames = bitmap.Frames;
        Frame = bitmap.Frame;
    }

    private Memory<byte> Buffer { get; }

    public bool HideRed { get; init; }
    public bool HideGreen { get; init; }
    public bool HideBlue { get; init; }
    public bool HideAlpha { get; init; }
    public int Frame { get; init; }

    public override double DpiX => 96;

    public override double DpiY => 96;

    public override PixelFormat Format => PixelFormats.Pbgra32;

    public override int PixelWidth => BackingPixelWidth;

    public override int PixelHeight => BackingPixelHeight;

    public override double Width => BackingPixelWidth;

    public override double Height => BackingPixelHeight;

    public override void CopyPixels(Int32Rect sourceRect, Array pixels, int stride, int offset) {
        var span = Buffer.Span[(int) (Width * Height * 4 * Frame)..];
        var pix = (byte[]) pixels;

        for (var y = sourceRect.Y; y < sourceRect.Y + sourceRect.Height; y++) {
            for (var x = sourceRect.X; x < sourceRect.X + sourceRect.Width; x++) {
                var i = stride * y + 4 * x;
                var a =  HideAlpha ? (byte) 0xFF : span[i + 3];
                pix[i + offset + 3] = a;
                pix[i + offset] = HideBlue ? (byte) 0 : (byte) (span[i + 0] * a / 0xFF);
                pix[i + offset + 1] = HideGreen ? (byte) 0 : (byte) (span[i + 1] * a / 0xFF);
                pix[i + offset + 2] = HideRed ? (byte) 0 : (byte) (span[i + 2] * a / 0xFF);
            }
        }
    }

    protected override Freezable CreateInstanceCore() => new BGRABitmapSource(Buffer, PixelWidth, PixelHeight, Frames) { Frame = Frame };

#pragma warning disable 67
    public override event EventHandler<DownloadProgressEventArgs>? DownloadProgress;
    public override event EventHandler? DownloadCompleted;
    public override event EventHandler<ExceptionEventArgs>? DownloadFailed;
    public override event EventHandler<ExceptionEventArgs>? DecodeFailed;
#pragma warning restore 67
}
