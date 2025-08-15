using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace APSSystem.Presentation.WPF.Converters;

/// <summary>
/// Converte um valor percentual de perda (Waste) em uma cor de fundo (Brush).
/// </summary>
public class WasteToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Converte o valor de entrada (que é decimal) para double para facilitar
        if (value is not decimal wastePercentage)
        {
            return Brushes.Transparent; // Cor padrão se o dado não for válido
        }

        // Converte o percentual para uma base de 0 a 100
        var wasteValue = wastePercentage; // O valor já vem como percentual (ex: 5.0 para 5%)

        if (wasteValue > 5.0m)
        {
            return new SolidColorBrush(Color.FromArgb(100, 255, 138, 128)); // Vermelho pastel
        }
        if (wasteValue < 1.5m)
        {
            return new SolidColorBrush(Color.FromArgb(100, 165, 214, 167)); // Verde pastel
        }
        // Se estiver entre 1.5% e 5%
        return new SolidColorBrush(Color.FromArgb(100, 255, 213, 128)); // Laranja/Amarelo pastel
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}