using APSSystem.Application.UseCases.AnalisarResultadoGams;
using MediatR;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WpfApp = System.Windows.Application;

// Usings para LiveCharts v2 (Abordagem Sections)
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace APSSystem.Presentation.WPF.ViewModels
{
    /// <summary>
    /// ViewModel responsável por apresentar os resultados detalhados da otimização.
    /// </summary>
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

        /// <summary>
        /// Inicializa uma nova instância de ResultadosOtimizacaoViewModel.
        /// </summary>
        /// <param name="mediator">O mediador para enviar comandos e queries.</param>
        public ResultadosOtimizacaoViewModel(IMediator mediator)
        {
            _mediator = mediator;
            XAxes = Array.Empty<Axis>();
            YAxes = Array.Empty<Axis>();
        }

        /// <summary>
        /// Carrega e processa os resultados da otimização a partir de um diretório de job.
        /// </summary>
        /// <param name="caminhoPastaJob">O caminho para a pasta contendo os arquivos de resultado do GAMS.</param>
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
                    // Ordena os dados antes de popular as coleções para garantir consistência na exibição.
                    var sortedPlanoCliente = resultado.PlanoCliente
                        .OrderBy(p => p.RequiredDate)
                        .ThenBy(p => p.Product)
                        .ThenBy(p => p.Length)
                        .ThenBy(p => p.CuttingWidth)
                        .ToList();

                    PlanoCliente.Clear();
                    sortedPlanoCliente.ForEach(item => PlanoCliente.Add(item));

                    var sortedPlanoProducao = resultado.PlanoProducao
                        .OrderBy(p => p.DataProducao)
                        .ThenBy(p => p.Maquina)
                        .ThenBy(p => p.JobNumber)
                        .ToList();

                    PlanoProducao.Clear();
                    sortedPlanoProducao.ForEach(item => PlanoProducao.Add(item));

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
        /// Constrói os dados do gráfico de Gantt com lógica de sequenciamento de jobs.
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

            // 1. ORDENAÇÃO: Ordena a carga de produção conforme a regra de negócio.
            var sortedJobs = PlanoProducao
                .OrderBy(p => p.DataProducao)
                .ThenBy(p => p.Maquina)
                .ToList();

            var machineLabels = sortedJobs
                .Select(p => p.Maquina)
                .Distinct()
                .OrderBy(m => m)
                .ToList();

            var machineRowMap = machineLabels.Select((machine, index) => new { machine, index })
                                             .ToDictionary(m => m.machine, m => m.index);

            const double halfHeight = 0.4;
            var random = new Random();
            var inicioProducao = sortedJobs.First().DataProducao;

            // 2. SEQUENCIAMENTO: Dicionário para rastrear o fim do último job em cada máquina.
            var machineTimelines = machineLabels.ToDictionary(m => m, m => inicioProducao);

            foreach (var job in sortedJobs)
            {
                // Calcula a duração real do job
                var duracaoEmHoras = job.QtdBobinasMae * 0.5;
                var duracao = TimeSpan.FromHours((double)duracaoEmHoras);

                // O job começa quando o último job da máquina terminou.
                var startTime = machineTimelines[job.Maquina];
                var endTime = startTime.Add(duracao);

                // Atualiza o tempo de término para a próxima tarefa nesta máquina.
                machineTimelines[job.Maquina] = endTime;

                GanttSections.Add(new RectangularSection
                {
                    Xi = startTime.ToOADate(),
                    Xj = endTime.ToOADate(),
                    Yi = machineRowMap[job.Maquina] - halfHeight,
                    Yj = machineRowMap[job.Maquina] + halfHeight,
                    Fill = new SolidColorPaint(new SKColor((byte)random.Next(50, 206), (byte)random.Next(50, 206), (byte)random.Next(50, 206), 180)),
                    Stroke = new SolidColorPaint(SKColors.Black) { StrokeThickness = 1 },
                    ZIndex = 2,
                    Label = job.JobNumber,
                    LabelPaint = new SolidColorPaint(SKColors.Black)
                });
            }

            // 3. VISUALIZAÇÃO: Cria uma seção de fundo para destacar o primeiro "dia".
            GanttSections.Add(new RectangularSection
            {
                Xi = inicioProducao.ToOADate(),
                Xj = inicioProducao.AddHours(8).ToOADate(),
                Fill = new SolidColorPaint(SKColors.LightGray) { ZIndex = -1 }, // ZIndex -1 para ficar no fundo
                Stroke = null
            });


            XAxes = new[]
            {
                new Axis
                {
                    Labeler = v => new DateTime((long)(v * TimeSpan.TicksPerDay + DateTime.Now.Ticks)).ToString("HH:mm"),
                    //Labeler = value => DateTime.FromOADate(value).ToString("dd > HH:mm"),
                    LabelsRotation = 0,
                    // Define a janela de visualização total para 16 horas.
                    MinLimit = inicioProducao.ToOADate(),
                    MaxLimit = inicioProducao.AddHours(16).ToOADate(),
                    UnitWidth = TimeSpan.FromHours(1).Ticks
                }
            };

            YAxes = new[]
            {
                new Axis
                {
                    Labels = machineLabels,
                    IsInverted = true,
                    MinLimit = -0.5,
                    MaxLimit = machineLabels.Count - 0.5
                }
            };
        }
    }
}