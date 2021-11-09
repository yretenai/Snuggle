using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Markup;
using Entropy.Handlers;
using Equilibrium.Implementations;

namespace Entropy.Converters {
    public class TextToStringConverter : MarkupExtension, IValueConverter {
        public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture) => value is not Text text ? null : new TaskCompletionNotifier<string?>(text, ConvertText(text));

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException($"{nameof(TextToStringConverter)} only supports converting to string");

        private static async Task<string?> ConvertText(Text text) {
            return await EntropyCore.Instance.WorkerAction("DecodeText",
                _ => {
                    if (text.ShouldDeserialize) {
                        text.Deserialize(EntropyCore.Instance.Settings.ObjectOptions);
                    }

                    return text.String;
                }, true);
        }

        public override object ProvideValue(IServiceProvider serviceProvider) => this;
    }
}
