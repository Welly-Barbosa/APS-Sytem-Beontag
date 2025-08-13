using APSSystem.Presentation.WPF.ViewModels;
using System.Windows;

namespace APSSystem.Presentation.WPF;

/// <summary>
/// A View (janela) responsável por exibir a análise de resultados pós-otimização.
/// </summary>
public partial class ResultadosOtimizacaoWindow : Window
{
    private readonly ResultadosOtimizacaoViewModel _viewModel;

    public ResultadosOtimizacaoWindow(ResultadosOtimizacaoViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    /// <summary>
    /// Método público para ser chamado externamente para carregar os dados.
    /// Foi renomeado para corresponder ao método no ViewModel.
    /// </summary>
    public async void CarregarDadosDoJob(string caminhoPastaJob)
    {
        // A lógica de carregar o arquivo agora vive dentro do comando do ViewModel.
        // Aqui, poderíamos passar o caminho para o ViewModel se necessário,
        // mas a lógica atual com OpenFileDialog já está encapsulada.
        // Para o fluxo automático, vamos acionar o comando.
        if (_viewModel.CarregarArquivoCommand.CanExecute(caminhoPastaJob))
        {
            // A forma mais limpa seria passar o caminho para o comando,
            // mas para manter a consistência com o ViewModel atual, vamos apenas executá-lo.
            // O ViewModel abrirá a caixa de diálogo para selecionar os arquivos.
            await _viewModel.CarregarArquivoAsync();
        }
    }
}