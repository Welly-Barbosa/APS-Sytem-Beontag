using APSSystem.Application.UseCases.AnalisarResultadoGams;
using MediatR;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace APSSystem.Presentation.WPF.ViewModels;

/// <summary>
/// ViewModel para a tela de análise de resultados pós-otimização.
/// </summary>
public class ResultadosOtimizacaoViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private string _statusMessage = "Loading results...";
    private bool _isIdle = false;

    public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(); } }
    public bool IsIdle { get => _isIdle; set { _isIdle = value; OnPropertyChanged(); } }

    // KPIs
    private decimal _orderFulfillment;
    public decimal OrderFulfillment { get => _orderFulfillment; set { _orderFulfillment = value; OnPropertyChanged(); } }
    private decimal _averageWaste;
    public decimal AverageWaste { get => _averageWaste; set { _averageWaste = value; OnPropertyChanged(); } }

    // Coleções para as tabelas
    public ObservableCollection<ItemDePlanoDetalhado> PlanoCliente { get; set; } = new();
    public ObservableCollection<ItemOrdemProducao> PlanoProducao { get; set; } = new();

    public ResultadosOtimizacaoViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Método público para carregar e analisar os dados de uma pasta de job.
    /// </summary>
    public async Task CarregarResultados(string caminhoPastaJob)
    {
        IsIdle = false;
        StatusMessage = "Parsing GAMS output files and analyzing results...";
        try
        {
            var command = new AnalisarResultadoGamsCommand(caminhoPastaJob);
            var resultado = await _mediator.Send(command);

            // Garante que a atualização da UI ocorra na thread correta
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                PlanoCliente.Clear();
                resultado.PlanoCliente.ForEach(item => PlanoCliente.Add(item));

                PlanoProducao.Clear();
                resultado.PlanoProducao.ForEach(item => PlanoProducao.Add(item));
            });

            // Atualiza os KPIs
            OrderFulfillment = resultado.OrderFulfillmentPercentage;
            AverageWaste = resultado.AverageWastePercentage;

            StatusMessage = $"Analysis complete. {resultado.PlanoCliente.Count} customer orders analyzed.";
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
}