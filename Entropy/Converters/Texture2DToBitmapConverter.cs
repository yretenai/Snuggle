using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Entropy.Handlers;
using Equilibrium.Implementations;

namespace Entropy.Converters {
    public class Texture2DToBitmapConverter : MarkupExtension, IValueConverter {
        public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture) => value is not Texture2D texture ? null : new TaskCompletionNotifier<BitmapSource?>(texture, ConvertTexture(texture, Dispatcher.CurrentDispatcher));

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException($"{nameof(Texture2DToBitmapConverter)} only supports converting to BitmapSource");

        private static async Task<BitmapSource?> ConvertTexture(Texture2D texture, Dispatcher dispatcher) {
            return await EntropyCore.Instance.WorkerAction("DecodeTexture",
                _ => {
                    if (texture.ShouldDeserialize) {
                        texture.Deserialize(EntropyCore.Instance.Settings.ObjectOptions);
                    }

                    var memory = EntropyTextureFile.LoadCachedTexture(texture);
                    return memory.IsEmpty ? null : dispatcher.Invoke(() => new RGBABitmapSource(memory, texture.Width, texture.Height));
                });
        }

        public override object ProvideValue(IServiceProvider serviceProvider) => this;
    }
}
