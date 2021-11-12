using System;
using System.Globalization;
using System.Windows.Data;
using Snuggle.Handlers;

namespace Snuggle.Converters;

public class RendererConverter : IValueConverter {
    public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture) {
        if (value == null) {
            return null;
        }

        if (value is not SnuggleObject SnuggleObject) {
            throw new NotSupportedException();
        }

        return SnuggleObject.GetObject();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
