using APSSystem.Presentation.WPF.ViewModels;
using System.Windows;

namespace APSSystem.Presentation.WPF;

public partial class ResultadosOtimizacaoWindow : Window
{
    private readonly ResultadosOtimizacaoViewModel _viewModel;

    // O construtor agora recebe o ViewModel via DI
    public ResultadosOtimizacaoWindow(ResultadosOtimizacaoViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel; // Conecta a View ao ViewModel
    }

    // Método para ser chamado externamente para carregar os dados
    public async void CarregarDadosDoJob(string caminhoPastaJob)
    {
        await _viewModel.CarregarResultados(caminhoPastaJob);
    }
}