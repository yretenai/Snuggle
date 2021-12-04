using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Snuggle.Core.Implementations;
using Snuggle.Handlers;

namespace Snuggle.Converters;

public class Texture2DToBitmapConverter : MarkupExtension, IValueConverter {
    public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture) => value is not Texture2D texture ? null : new TaskCompletionNotifier<BitmapSource?>(texture, ConvertTexture(texture, Dispatcher.CurrentDispatcher));

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException($"{nameof(Texture2DToBitmapConverter)} only supports converting to BitmapSource");

    private static async Task<BitmapSource?> ConvertTexture(Texture2D texture, Dispatcher dispatcher) {
        return await SnuggleCore.Instance.WorkerAction(
            "DecodeTexture",
            _ => {
                texture.Deserialize(SnuggleCore.Instance.Settings.ObjectOptions);
                var memory = SnuggleTextureFile.LoadCachedTexture(texture, SnuggleCore.Instance.Settings.ExportOptions.UseDirectTex);
                return memory.Length == 0 ? null : dispatcher.Invoke(() => new RGBABitmapSource(memory, texture.Width, texture.Height, texture.TextureFormat));
            },
            true);
    }

    public override object ProvideValue(IServiceProvider serviceProvider) => this;
}
