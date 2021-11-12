using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Markup;
using Snuggle.Core.Implementations;
using Snuggle.Handlers;

namespace Snuggle.Converters;

public class TextToStringConverter : MarkupExtension, IValueConverter {
    public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture) => value is not Text text ? null : new TaskCompletionNotifier<string?>(text, ConvertText(text));

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException($"{nameof(TextToStringConverter)} only supports converting to string");

    private static async Task<string?> ConvertText(Text text) {
        return await SnuggleCore.Instance.WorkerAction(
            "DecodeText",
            _ => {
                text.Deserialize(SnuggleCore.Instance.Settings.ObjectOptions);
                return text.String;
            },
            true);
    }

    public override object ProvideValue(IServiceProvider serviceProvider) => this;
}
