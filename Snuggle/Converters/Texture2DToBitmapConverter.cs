using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Snuggle.Core.Interfaces;
using Snuggle.Handlers;

namespace Snuggle.Converters;

public class Texture2DToBitmapConverter : MarkupExtension, IValueConverter {
    private CancellationTokenSource Token { get; set; } = new();
    public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture) {
        if (value is not ITexture texture) {
            return null;
        }

        Token.Cancel();
        Token.Dispose();
        Token = new CancellationTokenSource();
        return new TaskCompletionNotifier<BitmapSource?>(texture, ConvertTexture(texture, Dispatcher.CurrentDispatcher));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException($"{nameof(Texture2DToBitmapConverter)} only supports converting to BitmapSource");

    private async Task<BitmapSource?> ConvertTexture(ITexture texture, Dispatcher dispatcher) {
        return await SnuggleCore.Instance.WorkerAction(
            "DecodeTexture",
            _ => {
                texture.Deserialize(SnuggleCore.Instance.Settings.ObjectOptions);
                if (Token.IsCancellationRequested) {
                    return null;
                }
                var memory = SnuggleTextureFile.LoadCachedTexture(texture);
                return memory.Length == 0 ? null : dispatcher.Invoke(() => new BGRABitmapSource(memory, texture.Width, texture.Height, texture.Depth));
            },
            true, Token.Token);
    }

    public override object ProvideValue(IServiceProvider serviceProvider) => this;
}
