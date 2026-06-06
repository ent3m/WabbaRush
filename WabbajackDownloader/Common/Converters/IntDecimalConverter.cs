using Avalonia.Data.Converters;
using System.Globalization;

namespace WabbajackDownloader.Common.Converters;

internal sealed class IntDecimalConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int intValue)
            return (decimal)intValue;
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is decimal decimalValue)
            return (int)decimalValue;
        return null;
    }
}