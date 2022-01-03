using System;
using System.Globalization;
using System.Text.Json;
using System.Windows.Data;
using Snuggle.Core.Options;
using Snuggle.Handlers;

namespace Snuggle.Converters;

public class ObjectToJsonConverter : IValueConverter {
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) {
        if (targetType != typeof(string)) {
            throw new NotSupportedException($"{nameof(ObjectToJsonConverter)} only supports converting to string");
        }

        if (value is SnuggleObject SnuggleObject) {
            value = SnuggleObject.GetObject();
        }

        try {
            return value == null ? "{}" : JsonSerializer.Serialize(value, SnuggleCoreOptions.JsonOptions);
        } catch (Exception e) {
            SnuggleCore.Instance.LogTarget.Error("SerializedObject", "Failed converting object to JSON", e);
            return "{}";
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException($"{nameof(ObjectToJsonConverter)} only supports converting to string");
}
