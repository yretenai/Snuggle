using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Snuggle.Converters;

public class ScalingModeToText : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
        return (BitmapScalingMode) value switch {
            BitmapScalingMode.Linear => "Low",
            BitmapScalingMode.Fant => "High",
            BitmapScalingMode.NearestNeighbor => "Pixel",
            _ => "Unknown",
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
