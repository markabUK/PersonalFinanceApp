using Avalonia.Controls;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace PersonalFinanceApp.Desktop.Converters;

public class DoubleToGridLengthConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d)
        {
            return new GridLength(Math.Max(0.01, d), GridUnitType.Star);
        }
        return new GridLength(1, GridUnitType.Star);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}