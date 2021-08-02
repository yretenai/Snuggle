using System;
using System.Globalization;
using System.Text.Json;
using System.Windows.Data;
using Equilibrium.Options;

namespace Entropy.Converters {
    public class ObjectToJsonConverter : IValueConverter {
        public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) {
            if (targetType != typeof(string)) {
                throw new NotSupportedException($"{nameof(ObjectToJsonConverter)} only supports converting to string");
            }

            return value == null ? "{}" : JsonSerializer.Serialize(value, EquilibriumOptions.JsonOptions);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException($"{nameof(ObjectToJsonConverter)} only supports converting to string");
    }
}
