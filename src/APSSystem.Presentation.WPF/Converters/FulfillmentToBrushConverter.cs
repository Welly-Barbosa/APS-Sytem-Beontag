using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace APSSystem.Presentation.WPF.Converters
{
    /// <summary>
    /// Converte um valor percentual de atendimento de pedidos (Order Fulfillment) em um Brush de cor apropriado
    /// para formatação condicional.
    /// A lógica é:
    /// - Menor que 80% -> Vermelho (Ruim)
    /// - Maior que 95% -> Verde (Bom)
    /// - Entre 80% e 95% -> Laranja (Atenção)
    /// </summary>
    public class FulfillmentToBrushConverter : IValueConverter
    {
        /// <summary>
        /// Converte o valor de atendimento para uma cor.
        /// </summary>
        /// <param name="value">O valor do binding, esperado ser um double ou decimal representando a porcentagem (ex: 0.95 para 95%).</param>
        /// <param name="targetType">O tipo de destino (ignorado).</param>
        /// <param name="parameter">Parâmetro do conversor (ignorado).</param>
        /// <param name="culture">Informação de cultura (ignorada).</param>
        /// <returns>Um SolidColorBrush correspondente à regra de negócio.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not (double or decimal))
            {
                return Brushes.Black; // Cor padrão para tipos inesperados
            }

            var fulfillmentValue = System.Convert.ToDouble(value);

            // Se o atendimento for menor que 80% (0.80), é crítico.
            if (fulfillmentValue < 0.80)
            {
                return Brushes.Red;
            }
            // Se for maior que 95% (0.95), é ótimo.
            else if (fulfillmentValue > 0.95)
            {
                return Brushes.Green;
            }
            // Caso contrário, está na faixa de atenção.
            else
            {
                return Brushes.Orange;
            }
        }

        /// <summary>
        /// A conversão reversa não é suportada para este cenário.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("A conversão reversa não é necessária nem suportada.");
        }
    }
}