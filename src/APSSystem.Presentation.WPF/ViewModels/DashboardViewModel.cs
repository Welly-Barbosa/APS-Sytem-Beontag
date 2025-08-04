using APSSystem.Application.Interfaces;
using APSSystem.Application.UseCases.GerarArquivoGams;
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
using System.Windows; // <-- A DIRETIVA 'using' CRÍTICA
using System.Windows.Input;

namespace APSSystem.Presentation.WPF.ViewModels;

public class DashboardViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly IScenarioService _scenarioService;
    private string _statusMessage = "System ready. Select a scenario and click 'Analyze'.";
    private bool _isIdle = true;
    public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(); } }
    public bool IsIdle { get => _isIdle; set { _isIdle = value; OnPropertyChanged(); } }
    public CenarioTipo CenarioSelecionado { get; set; } = CenarioTipo.Antecipacao;
    public DateOnly DataInicio { get; set; } = DateOnly.FromDateTime(DateTime.Now);
    public int DuracaoHorizonteDias { get; set; } = 30;
    public DateOnly DataFim => DataInicio.AddDays(DuracaoHorizonteDias);
    private decimal _capacidadeTotal;
    public decimal CapacidadeTotal { get => _capacidadeTotal; set { _capacidadeTotal = value; OnPropertyChanged(); OnPropertyChanged(nameof(SaldoCapacidade)); } }
    private decimal _demandaTotal;
    public decimal DemandaTotal { get => _demandaTotal; set { _demandaTotal = value; OnPropertyChanged(); OnPropertyChanged(nameof(SaldoCapacidade)); } }
    public decimal SaldoCapacidade => CapacidadeTotal - DemandaTotal;
    public ObservableCollection<ISeries> Series { get; set; }
    public Axis[] XAxes { get; set; }
    public ObservableCollection<OrdemCliente> OrdensParaExibir { get; set; }
    public ICommand AnalisarCenarioCommand { get; }
    public ICommand StartOptimizationCommand { get; }

    public DashboardViewModel(IMediator mediator, IScenarioService scenarioService)
    {
        _mediator = mediator;
        _scenarioService = scenarioService;
        Series = new ObservableCollection<ISeries>();
        XAxes = new Axis[] { new Axis { LabelsRotation = 15, Labels = new string[0] } };
        OrdensParaExibir = new ObservableCollection<OrdemCliente>();
        AnalisarCenarioCommand = new RelayCommand(async _ => await ExecutarAnaliseCenario(), _ => IsIdle);
        StartOptimizationCommand = new RelayCommand(async _ => await ExecutarOtimizacao(), _ => IsIdle);
        OnPropertyChanged(nameof(DataFim));
        //Task.Run(async () => await ExecutarAnaliseCenario());
    }

    private async Task ExecutarAnaliseCenario()
    {
        IsIdle = false;
        StatusMessage = "Analyzing scenario, please wait...";
        try
        {
            _scenarioService.DefinirCenario(CenarioSelecionado);
            var query = new ObterDadosDashboardQuery(DataInicio, DataFim);
            var resultado = await _mediator.Send(query);
            var nomesDosRecursos = resultado.PontosDeDados.SelectMany(p => p.CapacidadePorRecurso.Keys).Distinct().ToList();
            var novasSeries = new List<ISeries>();
            foreach (var nomeRecurso in nomesDosRecursos)
            {
                novasSeries.Add(new StackedColumnSeries<decimal> { Name = nomeRecurso, Values = resultado.PontosDeDados.Select(p => p.CapacidadePorRecurso.GetValueOrDefault(nomeRecurso)) });
            }
            novasSeries.Add(new ColumnSeries<decimal> { Name = "Demand", Values = resultado.PontosDeDados.Select(p => p.DemandaTotal), Fill = null });
            var labelsEixoX = resultado.PontosDeDados.Select(p => p.Data.ToString("dd/MM")).ToArray();
            // COMETÁRIO INICIA AQUI 
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            //Application.Current.Dispatcher.Invoke(() =>
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