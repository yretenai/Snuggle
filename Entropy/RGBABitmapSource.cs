using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Entropy {
    public class RGBABitmapSource : BitmapSource {
        private readonly int BackingPixelWidth;
        private readonly int BackingPixelHeight;
        private byte[] Buffer { get; }

        public bool HideRed { get; set; }
        public bool HideGreen { get; set; }
        public bool HideBlue { get; set; }
        public bool HideAlpha { get; set; }

        public RGBABitmapSource(Span<byte> rgbaBuffer, int pixelWidth, int pixelHeight) {
            Buffer = rgbaBuffer.ToArray();
            BackingPixelWidth = pixelWidth;
            BackingPixelHeight = pixelHeight;
        }

        public override void CopyPixels(Int32Rect sourceRect, Array pixels, int stride, int offset) {
            for (var y = sourceRect.Y; y < sourceRect.Y + sourceRect.Height; y++) {
                for (var x = sourceRect.X; x < sourceRect.X + sourceRect.Width; x++) {
                    var i = stride * y + 4 * x;
                    var a = HideAlpha ? (byte) 0xFF : Buffer[i + 3];
                    var r = HideRed ? (byte) 0 : (byte) (Buffer[i] * a / 0xFF);
                    var g = HideGreen ? (byte) 0 : (byte) (Buffer[i + 1] * a / 0xFF);
                    var b = HideBlue ? (byte) 0 : (byte) (Buffer[i + 2] * a / 0xFF);

                    pixels.SetValue(b, i + offset);
                    pixels.SetValue(g, i + offset + 1);
                    pixels.SetValue(r, i + offset + 2);
                    pixels.SetValue(a, i + offset + 3);
                }
            }
        }

        protected override Freezable CreateInstanceCore() {
            return new RGBABitmapSource(Buffer, PixelWidth, PixelHeight);
        }

        public override event EventHandler<DownloadProgressEventArgs>? DownloadProgress;
        public override event EventHandler? DownloadCompleted;
        public override event EventHandler<ExceptionEventArgs>? DownloadFailed;
        public override event EventHandler<ExceptionEventArgs>? DecodeFailed;

        public override double DpiX => 96;

        public override double DpiY => 96;

        public override PixelFormat Format => PixelFormats.Pbgra32;

        public override int PixelWidth => BackingPixelWidth;

        public override int PixelHeight => BackingPixelHeight;

        public override double Width => BackingPixelWidth;

        public override double Height => BackingPixelHeight;
    }
}
