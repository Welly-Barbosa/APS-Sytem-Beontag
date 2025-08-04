using System;
using System.Globalization;
using System.Windows.Data;

namespace APSSystem.Presentation.WPF.Converters;

public class EnumToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value?.ToString() == null || parameter?.ToString() == null)
            return false;

        return value.ToString()!.Equals(parameter.ToString(), StringComparison.InvariantCultureIgnoreCase);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is false || parameter?.ToString() == null)
            return Binding.DoNothing;

        return Enum.Parse(targetType, parameter.ToString()!);
    }
}