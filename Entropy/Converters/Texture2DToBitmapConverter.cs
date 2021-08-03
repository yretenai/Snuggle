using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using DirectXTexNet;
using Entropy.ViewModels;
using Equilibrium.Extensions;
using Equilibrium.Implementations;
using Equilibrium.Options;

namespace Entropy.Converters {
    public class Texture2DToBitmapConverter : MarkupExtension, IValueConverter {
        public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture) => value is not Texture2D texture ? null : new TaskCompletionNotifier<BitmapImage?>(texture, ConvertTexture(texture));

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException($"{nameof(Texture2DToBitmapConverter)} only supports converting to BitmapImage");

        private static async Task<BitmapImage?> ConvertTexture(Texture2D texture) {
            return await EntropyCore.Instance.WorkerAction(_ => {
                if (texture.ShouldDeserialize) {
                    texture.Deserialize(ObjectDeserializationOptions.Default);
                }

                var bytes = default(byte[]);
                if (texture.TextureFormat.CanSupportDDS()) {
                    bytes = ConvertDDS(texture);
                } else if (texture.TextureFormat.IsASTC()) {
                    bytes = null;
                } else if (texture.TextureFormat.IsETC()) {
                    bytes = null;
                } else if (texture.TextureFormat.IsPVRTC()) {
                    bytes = null;
                }

                return BytesToBitmap(bytes);
            });
        }

        private static unsafe byte[]? ConvertDDS(Texture2D texture) {
            ScratchImage? scratch = null;
            try {
                var data = texture.ToDDS();
                fixed (byte* dataPin = data) {
                    scratch = TexHelper.Instance.LoadFromDDSMemory((IntPtr) dataPin, data.Length, DDS_FLAGS.NONE);
                    TexMetadata info = scratch.GetMetadata();

                    if (TexHelper.Instance.IsCompressed(info.Format)) {
                        ScratchImage temp = scratch.Decompress(0, DXGI_FORMAT.UNKNOWN);
                        scratch.Dispose();
                        scratch = temp;
                        info = scratch.GetMetadata();
                    }

                    if (info.Format != DXGI_FORMAT.R8G8B8A8_UNORM) {
                        ScratchImage temp = scratch.Convert(DXGI_FORMAT.R8G8B8A8_UNORM, TEX_FILTER_FLAGS.DEFAULT, 0.5f);
                        scratch.Dispose();
                        scratch = temp;
                    }

                    UnmanagedMemoryStream stream =
                        scratch.SaveToWICMemory(0, WIC_FLAGS.NONE, TexHelper.Instance.GetWICCodec(WICCodecs.PNG));

                    if (stream == null) {
                        return null;
                    }

                    byte[] tex = new byte[stream.Length];
                    stream.Read(tex, 0, tex.Length);
                    scratch.Dispose();

                    return tex;
                }
            } catch {
                if (scratch is { IsDisposed: false }) {
                    scratch.Dispose();
                }
            }

            return null;
        }

        private static BitmapImage? BytesToBitmap(byte[]? tex) {
            if (tex == null ||
                tex.Length == 0) {
                return null;
            }

            var bitmap = new BitmapImage();
            using (var ms = new MemoryStream(tex)) {
                ms.Position = 0;
                bitmap.BeginInit();
                bitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = null;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
            }

            bitmap.Freeze();
            return bitmap;
        }

        public override object ProvideValue(IServiceProvider serviceProvider) => this;
    }
}
