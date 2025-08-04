using System;
using System.Globalization;
using System.Windows.Data;

namespace APSSystem.Presentation.WPF.Converters;

public class DateOnlyToDateTimeConverter : IValueConverter
{
    public static readonly DateOnlyToDateTimeConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateOnly dateOnly)
        {
            return dateOnly.ToDateTime(TimeOnly.MinValue);
        }
        return null!; // Retorna nulo se o valor de entrada não for DateOnly
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime dateTime)
        {
            return DateOnly.FromDateTime(dateTime);
        }
        return null!; // Retorna nulo se o valor de entrada não for DateTime
    }
}