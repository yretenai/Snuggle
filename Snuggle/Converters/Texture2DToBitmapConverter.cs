using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Snuggle.Core.Interfaces;
using Snuggle.Handlers;

namespace Snuggle.Converters;

public class Texture2DToBitmapConverter : MarkupExtension, IValueConverter {
    public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture) => value is not ITexture texture ? null : new TaskCompletionNotifier<BitmapSource?>(texture, ConvertTexture(texture, Dispatcher.CurrentDispatcher));

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException($"{nameof(Texture2DToBitmapConverter)} only supports converting to BitmapSource");

    private static async Task<BitmapSource?> ConvertTexture(ITexture texture, Dispatcher dispatcher) {
        return await SnuggleCore.Instance.WorkerAction(
            "DecodeTexture",
            _ => {
                texture.Deserialize(SnuggleCore.Instance.Settings.ObjectOptions);
                var shouldUseDTK = OperatingSystem.IsWindows() && texture.Depth > 1 || SnuggleCore.Instance.Settings.ExportOptions.UseDirectTex;
                var memory = SnuggleTextureFile.LoadCachedTexture(texture, shouldUseDTK, SnuggleCore.Instance.Settings.ExportOptions.UseTextureDecoder);
                return memory.Length == 0 ? null : dispatcher.Invoke(() => new RGBABitmapSource(memory, texture.Width, texture.Height, texture.TextureFormat, texture.Depth));
            },
            true);
    }

    public override object ProvideValue(IServiceProvider serviceProvider) => this;
}
