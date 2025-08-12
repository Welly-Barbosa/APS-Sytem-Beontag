using APSSystem.Application.Interfaces;
using APSSystem.Application.UseCases.GerarArquivoGams;
using APSSystem.Application.UseCases.IniciarOtimizacao;
using APSSystem.Application.UseCases.ObterDadosDashboard;
using APSSystem.Core.Entities;
using APSSystem.Core.Enums;
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
using System.Windows;
using System.Windows.Input;

namespace APSSystem.Presentation.WPF.ViewModels;

public class DashboardViewModel : ViewModelBase
{
    public event Action<GamsExecutionResult>? OptimizationCompleted;
    private readonly IMediator _mediator;
    private readonly IScenarioService _scenarioService;
    private readonly IExcelDataService _excelDataService;

    private readonly Dictionary<string, SKColor> _resourceColorMap = new()
    {
        { "ASHE1", SKColors.CornflowerBlue },
        { "ASHE2", SKColors.MediumPurple },
        { "ATLAS", SKColors.LightSteelBlue },
    };

    private string _statusMessage = "System ready. Select a scenario and click 'Analyze'.";
    public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(); } }
    private bool _isIdle = true;
    public bool IsIdle { get => _isIdle; set { _isIdle = value; OnPropertyChanged(); } }

    public CenarioTipo CenarioSelecionado { get; set; } = CenarioTipo.Antecipacao;
    private DateOnly _dataInicio = DateOnly.FromDateTime(DateTime.Now);
    public DateOnly DataInicio { get => _dataInicio; set { _dataInicio = value; OnPropertyChanged(); OnPropertyChanged(nameof(DataFim)); OnPropertyChanged(nameof(DataFimLabel)); } }
    private int _duracaoHorizonteDias = 7;
    public int DuracaoHorizonteDias { get => _duracaoHorizonteDias; set { _duracaoHorizonteDias = value; OnPropertyChanged(); OnPropertyChanged(nameof(DataFim)); OnPropertyChanged(nameof(DuracaoLabel)); OnPropertyChanged(nameof(DataFimLabel)); } }
    public DateOnly DataFim => DataInicio.AddDays(DuracaoHorizonteDias);
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
        _excelDataService = excelDataService;

        Series = new ObservableCollection<ISeries>();
        OrdensParaExibir = new ObservableCollection<OrdemCliente>();
        XAxes = new Axis[] { new Axis { Name = "Days", LabelsRotation = 15, Labels = new string[0], SeparatorsPaint = new SolidColorPaint(SKColors.LightGray) { StrokeThickness = 1 } } };
        YAxes = new Axis[] { new Axis { Name = "Time (Minutes)", MinLimit = 0, MaxLimit = 5000 } };

        AnalisarCenarioCommand = new RelayCommand(async _ => await ExecutarAnaliseCenario(), _ => IsIdle);
        StartOptimizationCommand = new RelayCommand(async _ => await ExecutarOtimizacao(), _ => IsIdle);
    }

    private async Task ExecutarAnaliseCenario()
    {
        IsIdle = false;
        StatusMessage = "Loading and analyzing scenario data...";
        try
        {
            await _excelDataService.PreloadScenarioDataAsync(CenarioSelecionado);
            var query = new ObterDadosDashboardQuery(DataInicio, DataFim);
            var resultado = await _mediator.Send(query);

            var nomesDosRecursos = resultado.PontosDeDados.SelectMany(p => p.CapacidadePorRecurso.Keys).Distinct().OrderBy(r => r).ToList();
            var novasSeries = new List<ISeries>();

            foreach (var nomeRecurso in nomesDosRecursos)
            {
                var corDoRecurso = _resourceColorMap.GetValueOrDefault(nomeRecurso, SKColors.Gray);
                var valoresCapacidade = resultado.PontosDeDados.Select(p => (double)p.CapacidadePorRecurso.GetValueOrDefault(nomeRecurso)).ToList();
                novasSeries.Add(new StackedColumnSeries<double>
                {
                    Name = nomeRecurso,
                    Values = valoresCapacidade,
                    StackGroup = 1,
                    Fill = new SolidColorPaint(corDoRecurso)
                });
            }

            var valoresDemandaAtendida = resultado.PontosDeDados.Select(p => (double)(p.DemandaTotal <= p.CapacidadePorRecurso.Values.Sum() ? p.DemandaTotal : 0)).ToList();
            var valoresDemandaBacklog = resultado.PontosDeDados.Select(p => (double)(p.DemandaTotal > p.CapacidadePorRecurso.Values.Sum() ? p.DemandaTotal : 0)).ToList();

            novasSeries.Add(new StackedColumnSeries<double> { Name = "Demand (Met)", Values = valoresDemandaAtendida, StackGroup = 2, Fill = new SolidColorPaint(SKColors.DarkSeaGreen) });
            novasSeries.Add(new StackedColumnSeries<double> { Name = "Demand (Backlog)", Values = valoresDemandaBacklog, StackGroup = 2, Fill = new SolidColorPaint(SKColors.OrangeRed), IsVisibleAtLegend = false });

            var labelsEixoX = resultado.PontosDeDados.Select(p => p.Data.ToString("dd/MM")).ToArray();
            var separadores = Enumerable.Range(0, labelsEixoX.Length + 1).Select(i => i - 0.5).ToArray();

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                Series.Clear();
                novasSeries.ForEach(s => Series.Add(s));
                XAxes[0].Labels = labelsEixoX;
                XAxes[0].CustomSeparators = separadores; // <-- Propriedade Corrigida
                OrdensParaExibir.Clear();
                resultado.OrdensNoHorizonte.ForEach(o => OrdensParaExibir.Add(o));
            });

            CapacidadeTotal = resultado.CapacidadeTotalGeral;
            DemandaTotal = resultado.DemandaTotalGeral;
            StatusMessage = $"Analysis complete. {resultado.OrdensNoHorizonte.Count} orders loaded.";
            OnPropertyChanged(nameof(SaldoCapacidade));
        }
        catch (Exception ex) { StatusMessage = $"ERROR: {ex.Message}"; }
        finally { IsIdle = true; }
    }

    private async Task ExecutarOtimizacao()
    {
        IsIdle = false;
        StatusMessage = "Waiting for Optimization...";
        try
        {
            var command = new IniciarOtimizacaoCommand(CenarioSelecionado, DataInicio, DataFim);
            var resultadoExecucao = await _mediator.Send(command);
            StatusMessage = "Optimization complete! Opening results...";
            OptimizationCompleted?.Invoke(resultadoExecucao);
        }
        catch (Exception ex)
        {
            StatusMessage = $"ERROR: See dialog for details.";
            var errorDialog = new ErrorWindow(ex.Message);
            errorDialog.ShowDialog();
        }
        finally { IsIdle = true; }
    }
}