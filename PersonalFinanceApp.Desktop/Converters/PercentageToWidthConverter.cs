using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace PersonalFinanceApp.Desktop.Converters;

public class PercentageToWidthConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double percentage)
        {
            // Scale 0-100% to a maximum bar width of 120 pixels
            return Math.Clamp(percentage * 1.2, 2, 120);
        }
        return 2.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}