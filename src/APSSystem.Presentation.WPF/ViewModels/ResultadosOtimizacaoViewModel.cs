using APSSystem.Application.UseCases.AnalisarResultadoGams;
using MediatR;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WpfApp = System.Windows.Application;

// Usings para LiveCharts v2 (Abordagem Sections)
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using SkiaSharp;

namespace APSSystem.Presentation.WPF.ViewModels
{
    public class ResultadosOtimizacaoViewModel : ViewModelBase
    {
        private readonly IMediator _mediator;

        private string _statusMessage = "Ready";
        public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(); } }

        private bool _isIdle = true;
        public bool IsIdle { get => _isIdle; set { _isIdle = value; OnPropertyChanged(); } }

        // KPIs
        private decimal _orderFulfillment;
        public decimal OrderFulfillment { get => _orderFulfillment; set { _orderFulfillment = value; OnPropertyChanged(); } }

        private decimal _averageWaste;
        public decimal AverageWaste { get => _averageWaste; set { _averageWaste = value; OnPropertyChanged(); } }

        // Coleções para as tabelas
        public ObservableCollection<ItemDePlanoDetalhado> PlanoCliente { get; set; } = new();
        public ObservableCollection<ItemOrdemProducao> PlanoProducao { get; set; } = new();

        // PROPRIEDADES PARA O GRÁFICO (USANDO SECTIONS)
        public ObservableCollection<RectangularSection> GanttSections { get; } = new();
        private Axis[] _xAxes;
        public Axis[] XAxes { get => _xAxes; private set { _xAxes = value; OnPropertyChanged(); } }
        private Axis[] _yAxes;
        public Axis[] YAxes { get => _yAxes; private set { _yAxes = value; OnPropertyChanged(); } }

        public ResultadosOtimizacaoViewModel(IMediator mediator)
        {
            _mediator = mediator;
            XAxes = Array.Empty<Axis>();
            YAxes = Array.Empty<Axis>();
        }

        public async Task CarregarResultados(string caminhoPastaJob)
        {
            IsIdle = false;
            StatusMessage = "Parsing GAMS output files and analyzing results...";
            try
            {
                var command = new AnalisarResultadoGamsCommand(caminhoPastaJob);
                var resultado = await _mediator.Send(command);

                await WpfApp.Current.Dispatcher.InvokeAsync(() =>
                {
                    PlanoCliente.Clear();
                    resultado.PlanoCliente.ForEach(item => PlanoCliente.Add(item));

                    PlanoProducao.Clear();
                    resultado.PlanoProducao.ForEach(item => PlanoProducao.Add(item));

                    OrderFulfillment = resultado.OrderFulfillmentPercentage;
                    AverageWaste = resultado.AverageWastePercentage;

                    BuildGanttChartData();

                    StatusMessage = $"Analysis complete. {resultado.PlanoCliente.Count} customer orders analyzed.";
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"ERROR loading results: {ex.Message}";
                MessageBox.Show(ex.Message, "Error Analyzing Results", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsIdle = true;
            }
        }

        /// <summary>
        /// Constrói os dados do gráfico de Gantt (PoC) usando RectangularSections,
        /// com lógica robusta para garantir a visualização correta.
        /// </summary>
        private void BuildGanttChartData()
        {
            GanttSections.Clear();

            if (PlanoProducao is null || PlanoProducao.Count == 0)
            {
                XAxes = Array.Empty<Axis>();
                YAxes = Array.Empty<Axis>();
                return;
            }

            var groups = PlanoProducao
                .GroupBy(p => p.Maquina)
                .OrderBy(g => g.Key)
                .ToList();

            var machineLabels = groups.Select(g => g.Key ?? "Unknown").ToArray();
            const double halfHeight = 0.4;
            var random = new Random();

            // ALTERAÇÃO: Calcular limites de data para garantir que o gráfico sempre mostre os dados
            var minDate = PlanoProducao.Min(p => p.DataProducao);
            var maxDate = PlanoProducao.Max(p => p.DataProducao.AddHours(8)); // Assume duração de 8h

            int row = 0;
            foreach (var group in groups)
            {
                foreach (var job in group.OrderBy(j => j.DataProducao))
                {
                    var start = job.DataProducao;
                    var end = start.AddHours(8); // Duração fixa para PoC

                    GanttSections.Add(new RectangularSection
                    {
                        Xi = start.ToOADate(),
                        Xj = end.ToOADate(),
                        Yi = row - halfHeight,
                        Yj = row + halfHeight,
                        Fill = new SolidColorPaint(new SKColor((byte)random.Next(50, 206), (byte)random.Next(50, 206), (byte)random.Next(50, 206), 180)),
                        Stroke = new SolidColorPaint(SKColors.Black) { StrokeThickness = 1 },
                        // ALTERAÇÃO: ZIndex e LabelPaint para melhor legibilidade
                        ZIndex = 2,
                        Label = job.JobNumber,
                        LabelPaint = new SolidColorPaint(SKColors.Black)
                    });
                }
                row++;
            }

            XAxes = new[]
            {
                new Axis
                {
                    // ALTERAÇÃO: Simplificação do rótulo da data
                    Labeler = v => DateTime.FromOADate(v).ToString("dd/MM"),
                    LabelsRotation = 0,
                    // ALTERAÇÃO: Definição explícita dos limites do eixo
                    MinLimit = minDate.AddDays(-1).ToOADate(),
                    MaxLimit = maxDate.AddDays(1).ToOADate()
                }
            };

            YAxes = new[]
            {
                new Axis
                {
                    Labels = machineLabels,
                    IsInverted = true,
                    MinLimit = -0.5,
                    MaxLimit = machineLabels.Length - 0.5
                }
            };
        }
    }
}