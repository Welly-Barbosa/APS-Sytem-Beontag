using APSSystem.Application.UseCases.AnalisarResultadoGams;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using MediatR;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace APSSystem.Presentation.WPF.ViewModels;

public class ResultadosOtimizacaoViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private string _statusMessage = "Loading results...";
    public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(); } }

    // KPIs
    public decimal OrderFulfillmentPercentage { get; set; }
    public decimal AverageWastePercentage { get; set; }

    // Coleções para as Tabelas
    public ObservableCollection<ItemDePlanoDetalhado> PlanoCliente { get; set; } = new();
    public ObservableCollection<ItemOrdemProducao> PlanoProducao { get; set; } = new();

    // Propriedades para o Gráfico de Gantt
    public ObservableCollection<ISeries> SeriesGantt { get; set; } = new();
    public Axis[] YAxesGantt { get; set; }
    public Axis[] XAxesGantt { get; set; }


    public ResultadosOtimizacaoViewModel(IMediator mediator)
    {
        _mediator = mediator;
        // Configuração inicial dos eixos do Gantt
        // O Eixo Y mostrará os nomes das máquinas
        YAxesGantt = new Axis[]
        {
            new Axis
            {
                IsInverted = true, // Inverte o eixo para a máquina 1 ficar no topo
                Labels = new List<string>(),
                LabelsPaint = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SkiaSharp.SKColors.Black)
            }
        };
        // O Eixo X mostrará as datas
        XAxesGantt = new Axis[]
        {
            new Axis
            {
                UnitWidth = TimeSpan.FromDays(1).Ticks,
                MinStep = TimeSpan.FromDays(1).Ticks,
                Labeler = value => new DateTime((long)value).ToString("dd/MM/yyyy")
            }
        };
    }

    public async Task CarregarResultados(string caminhoPastaJob)
    {
        StatusMessage = "Parsing and analyzing GAMS results...";
        try
        {
            var command = new AnalisarResultadoGamsCommand(caminhoPastaJob);
            var resultado = await _mediator.Send(command);

            var ganttSeries = new List<ISeries>();
            var maquinas = resultado.PlanoProducao.Select(p => p.Maquina).Distinct().ToList();

            for (int i = 0; i < maquinas.Count; i++)
            {
                var maquina = maquinas[i];
                // Usando a robusta RowSeries para construir o Gantt
                ganttSeries.Add(new RowSeries<Core.Entities.PlanoDeProducaoItem>
                {
                    Name = maquina,
                    Values = resultado.PlanoProducao.Where(p => p.Maquina == maquina).ToList(),
                    // Mapeia nosso objeto para as coordenadas do gráfico
                    Mapping = (planoItem, chartPoint) =>
                    {
                        // O valor X (início da barra) é a Data de Produção
                        chartPoint.Coordinate.X = planoItem.DataProducao.Ticks;
                        // O valor Primário (comprimento da barra) é de 1 dia (simplificação visual)
                        chartPoint.Coordinate.PrimaryValue = TimeSpan.FromDays(1).Ticks;
                        // O valor Y (em qual "linha" desenhar) é o índice da máquina
                        chartPoint.Coordinate.Y = i;
                    },
                    DataLabelsPaint = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SkiaSharp.SKColors.White),
                    DataLabelsFormatter = (point) => ((Core.Entities.PlanoDeProducaoItem)point.Model!).PadraoCorte
                });
            }

            // Garante que a atualização da UI ocorra na thread correta
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                PlanoCliente.Clear();
                resultado.PlanoCliente.ForEach(item => PlanoCliente.Add(item));

                PlanoProducao.Clear();
                resultado.PlanoProducao.ForEach(item => PlanoProducao.Add(item));

                SeriesGantt.Clear();
                ganttSeries.ForEach(s => SeriesGantt.Add(s));

                // Atualiza os labels do eixo Y para serem os nomes das máquinas
                YAxesGantt[0].Labels = maquinas;
            });

            OrderFulfillmentPercentage = resultado.OrderFulfillmentPercentage;
            AverageWastePercentage = resultado.AverageWastePercentage;
            StatusMessage = "Analysis complete.";
            OnPropertyChanged(nameof(OrderFulfillmentPercentage));
            OnPropertyChanged(nameof(AverageWastePercentage));
        }
        catch (Exception ex)
        {
            StatusMessage = $"ERROR loading results: {ex.Message}";
            MessageBox.Show(ex.Message, "Error Analyzing Results", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}