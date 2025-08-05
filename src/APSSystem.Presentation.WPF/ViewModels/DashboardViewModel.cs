using APSSystem.Application.Interfaces;
using APSSystem.Application.UseCases.GerarArquivoGams;
using APSSystem.Application.UseCases.ObterDadosDashboard;
using APSSystem.Core.Entities;
using APSSystem.Core.Enums;
using APSSystem.Infrastructure.Services;
using APSSystem.Presentation.WPF.Commands;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using MediatR;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows; // <-- A DIRETIVA 'using' CRÍTICA
using System.Windows.Input;

namespace APSSystem.Presentation.WPF.ViewModels;

public class DashboardViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly IScenarioService _scenarioService;
    private string _statusMessage = "System ready. Select a scenario and click 'Analyze'.";
    private bool _isIdle = true;
    private readonly IExcelDataService _excelDataService; // Injeta o novo serviço

    // MUDANÇA: Horizonte padrão agora é 7 dias
    private int _duracaoHorizonteDias = 7;
    public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(); } }
    public bool IsIdle { get => _isIdle; set { _isIdle = value; OnPropertyChanged(); } }
    public CenarioTipo CenarioSelecionado { get; set; } = CenarioTipo.Antecipacao;
    public DateOnly DataInicio { get; set; } = DateOnly.FromDateTime(DateTime.Now);
    public int DuracaoHorizonteDias { get; set; } = 30;
    public DateOnly DataFim => DataInicio.AddDays(DuracaoHorizonteDias);
    // --- PROPRIEDADES DE TEXTO FORMATADO PARA A UI (CORREÇÃO DO BUG DO SLIDER) ---
    public string DuracaoLabel => $"Duration: {DuracaoHorizonteDias} days";
    public string DataFimLabel => DataFim.ToString("d");


    private decimal _capacidadeTotal;
    public decimal CapacidadeTotal { get => _capacidadeTotal; set { _capacidadeTotal = value; OnPropertyChanged(); OnPropertyChanged(nameof(SaldoCapacidade)); } }
    private decimal _demandaTotal;
    public decimal DemandaTotal { get => _demandaTotal; set { _demandaTotal = value; OnPropertyChanged(); OnPropertyChanged(nameof(SaldoCapacidade)); } }
    public decimal SaldoCapacidade => CapacidadeTotal - DemandaTotal;
    public ObservableCollection<ISeries> Series { get; set; }
    public Axis[] XAxes { get; set; }
    public Axis[] YAxes { get; set; }
    public ObservableCollection<OrdemCliente> OrdensParaExibir { get; set; }
    public ICommand AnalisarCenarioCommand { get; }
    public ICommand StartOptimizationCommand { get; }

    public DashboardViewModel(IMediator mediator, IScenarioService scenarioService, IExcelDataService excelDataService)
    {
        _mediator = mediator;
        _scenarioService = scenarioService;
        _excelDataService = excelDataService; // Armazena a referência
        Series = new ObservableCollection<ISeries>();
        OrdensParaExibir = new ObservableCollection<OrdemCliente>();

        XAxes = new Axis[] { new Axis { LabelsRotation = 15, Labels = new string[0] } };

        // --- INICIALIZAÇÃO DO EIXO Y COM LIMITE MÁXIMO ---
        YAxes = new Axis[] { new Axis { Name = "Time (Minutes)", MinLimit = 0, MaxLimit = 5000 } }; // Limite máximo fixo em 5000

        OrdensParaExibir = new ObservableCollection<OrdemCliente>();
        // --- ALTERAÇÃO 2: ADICIONANDO AS LINHAS SEPARADORAS ---
        XAxes = new Axis[]
        {
            new Axis
            {
                Name = "Days",
                LabelsRotation = 15,
                Labels = new string[0],
                // Esta linha desenha o separador vertical para cada dia
                SeparatorsPaint = new SolidColorPaint(SKColors.LightGray) { StrokeThickness = 1 }
            }
        };

        AnalisarCenarioCommand = new RelayCommand(async _ => await ExecutarAnaliseCenario(), _ => IsIdle);
        StartOptimizationCommand = new RelayCommand(async _ => await ExecutarOtimizacao(), _ => IsIdle);
        OnPropertyChanged(nameof(DataFim));
        Task.Run(async () => await ExecutarAnaliseCenario());
    }

    private async Task ExecutarAnaliseCenario()
    {
        IsIdle = false;
        StatusMessage = "Loading scenario data, please wait...";
        try
        {
                       // MUDANÇA: Pré-carrega os dados do Excel UMA ÚNICA VEZ
            await _excelDataService.PreloadScenarioDataAsync(CenarioSelecionado);

            StatusMessage = "Analyzing data...";
  
            var query = new ObterDadosDashboardQuery(DataInicio, DataFim);
            var resultado = await _mediator.Send(query);
            var nomesDosRecursos = resultado.PontosDeDados.SelectMany(p => p.CapacidadePorRecurso.Keys).Distinct().ToList();
            var novasSeries = new List<ISeries>();

            // Cria uma série empilhada para cada recurso, todas no mesmo grupo
            foreach (var nomeRecurso in nomesDosRecursos)
            {
                novasSeries.Add(new StackedColumnSeries<double>
                {
                    Name = nomeRecurso,
                    Values = resultado.PontosDeDados.Select(p => (double)p.CapacidadePorRecurso.GetValueOrDefault(nomeRecurso)),
                    StackGroup = 1, // Designa todas as capacidades para o Grupo de Empilhamento 1
                    YToolTipLabelFormatter = pt => $"{pt.Coordinate.PrimaryValue:0}"
                });
            }
            // --- LÓGICA DE COR DINÂMICA REFINADA ---

            // 1. Cria a série para a Demanda Atendida (verde)
            novasSeries.Add(new StackedColumnSeries<double>
            {
                Name = "Demand (Met)",
                Values = resultado.PontosDeDados.Select(p =>
                    p.DemandaTotal <= p.CapacidadePorRecurso.Values.Sum() ? (double)p.DemandaTotal : 0),
                StackGroup = 2,
                Fill = new SolidColorPaint(SKColors.DarkSeaGreen),
                YToolTipLabelFormatter = pt => $"{pt.Coordinate.PrimaryValue:0}"
                //DataLabelsFormatter = pt => $"{pt.Coordinate.PrimaryValue:0}"
            });

            // 2. Cria a série para a Demanda em Atraso (vermelho)
            novasSeries.Add(new StackedColumnSeries<double>
            {
                Name = "Demand (Backlog)",
                Values = resultado.PontosDeDados.Select(p =>
                    p.DemandaTotal > p.CapacidadePorRecurso.Values.Sum() ? (double)p.DemandaTotal : 0),
                StackGroup = 2, // Mesmo grupo da outra demanda, para que apareçam na mesma posição
                Fill = new SolidColorPaint(SKColors.OrangeRed),
                YToolTipLabelFormatter = pt => $"{pt.Coordinate.PrimaryValue:0}"
            });

            var labelsEixoX = resultado.PontosDeDados.Select(p => p.Data.ToString("dd/MM")).ToArray();
            // --- CÓDIGO NOVO PARA POSICIONAR OS SEPARADORES ---
            // Cria um array para definir a posição de cada separador
            var separadores = Enumerable
                .Range(0, labelsEixoX.Length + 1)
                .Select(i => i * 2 - 0.5) // duas colunas por dia = 2 slots
                .ToArray();


            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                Series.Clear();
                novasSeries.ForEach(s => Series.Add(s));
                XAxes[0].Labels = labelsEixoX;
                OrdensParaExibir.Clear();
                resultado.OrdensNoHorizonte.ForEach(o => OrdensParaExibir.Add(o));
            });
            // COMETÁRIO TERMINA AQUI
            CapacidadeTotal = resultado.CapacidadeTotalGeral;
            DemandaTotal = resultado.DemandaTotalGeral;
            StatusMessage = $"Analysis complete. {resultado.OrdensNoHorizonte.Count} orders loaded for the selected horizon.";
            OnPropertyChanged(nameof(SaldoCapacidade));
        }
        catch (Exception ex) { StatusMessage = $"ERROR during analysis: {ex.Message}"; }
        finally { IsIdle = true; }
    }

    private async Task ExecutarOtimizacao()
    {
        IsIdle = false;
        StatusMessage = "Starting optimization... Generating GAMS file.";
        try
        {
            _scenarioService.DefinirCenario(CenarioSelecionado);
            string caminhoDeSaida = Path.Combine(AppContext.BaseDirectory, "GamsInputData.dat");
            var command = new GerarArquivoGamsCommand(caminhoDeSaida, DataInicio, DataFim);
            await _mediator.Send(command);
            StatusMessage = "GAMS input file generated. Simulating optimization run...";
            await Task.Delay(3000);
            //var resultadosView = new ResultadosOtimizacaoWindow();
            //resultadosView.Show();
            StatusMessage = "Optimization complete. Results window would open.";
        }
        catch (Exception ex) { StatusMessage = $"ERROR during optimization: {ex.Message}"; }
        finally { IsIdle = true; }
    }
}