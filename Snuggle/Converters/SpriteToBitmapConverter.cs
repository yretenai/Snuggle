using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Snuggle.Core.Implementations;
using Snuggle.Handlers;

namespace Snuggle.Converters;

public sealed class SpriteToBitmapConverter : MarkupExtension, IValueConverter, IDisposable {
    private CancellationTokenSource Token { get; set; } = new();
    public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture) {
        if (value is not Sprite sprite) {
            return null;
        }

        Token.Cancel();
        Token.Dispose();
        Token = new CancellationTokenSource();

        return new TaskCompletionNotifier<BitmapSource?>(sprite, ConvertSprite(sprite, Dispatcher.CurrentDispatcher));
    }
    
    ~SpriteToBitmapConverter() {
        DisposeInner();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException($"{nameof(Texture2DToBitmapConverter)} only supports converting to BitmapSource");

    private async Task<BitmapSource?> ConvertSprite(Sprite sprite, Dispatcher dispatcher) {
        return await SnuggleCore.Instance.WorkerAction(
            "DecodeSprite",
            _ => {
                sprite.Deserialize(SnuggleCore.Instance.Settings.ObjectOptions);
                if (Token.IsCancellationRequested) {
                    return null;
                }
                var (memory, (width, height), _) = SnuggleSpriteFile.ConvertSprite(sprite, SnuggleCore.Instance.Settings.ObjectOptions);
                return memory.Length == 0 ? null : dispatcher.Invoke(() => new BGRABitmapSource(memory, width, height, 1));
            },
            true, Token.Token);
    }

    public override object ProvideValue(IServiceProvider serviceProvider) => this;
    public void Dispose() {
        DisposeInner();
        GC.SuppressFinalize(this);
    }

    private void DisposeInner() {
        Token.Cancel();
        Token.Dispose();
    }
}
