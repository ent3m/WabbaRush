using Avalonia.Data.Converters;
using System.Globalization;

namespace WabbajackDownloader.Common.Converters;

internal sealed class IntDoubleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int intValue)
            return (double)intValue;
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double decimalValue)
            return (int)decimalValue;
        return null;
    }
}