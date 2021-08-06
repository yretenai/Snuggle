using System;
using System.Globalization;
using System.Windows.Data;
using Entropy.Handlers;

namespace Entropy.Converters {
    public class RendererConverter : IValueConverter {
        public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture) {
            if (value == null) {
                return null;
            }

            if (value is not EntropyObject entropyObject) {
                throw new NotSupportedException();
            }

            return entropyObject.GetObject();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
