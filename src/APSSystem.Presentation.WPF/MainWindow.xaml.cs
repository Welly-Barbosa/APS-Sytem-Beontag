using APSSystem.Application.Interfaces;
using APSSystem.Presentation.WPF.ViewModels;
using APSSystem.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;

namespace APSSystem.Presentation.WPF
{
    /// <summary>
    /// Janela principal do sistema. Orquestra a navegação e reage à conclusão da otimização.
    /// </summary>
    public partial class MainWindow : Window
    {
        // ===========================
        // Campos privados
        // ===========================
        private readonly IServiceProvider serviceProvider;

        // ===========================
        // Construtores
        // ===========================

        /// <summary>
        /// Cria uma instância da janela principal com o ViewModel do Dashboard e o provedor de serviços.
        /// </summary>
        /// <param name="viewModel">ViewModel do dashboard, injetado via DI.</param>
        /// <param name="serviceProvider">Provedor de serviços (DI) para resolver janelas/serviços auxiliares.</param>
        public MainWindow(DashboardViewModel viewModel, IServiceProvider serviceProvider)
        {
            InitializeComponent();

            // Guard clause
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            // Assina evento de conclusão da otimização para abrir a janela de resultados
            viewModel.OptimizationCompleted += OnOptimizationCompleted;

            // Define o DataContext do Dashboard
            DataContext = viewModel;
        }

        // ===========================
        // Métodos públicos
        // ===========================

        /// <summary>
        /// Handler do botão "Abrir Resultados" no Dashboard.
        /// Abre a janela de resultados e dispara o carregamento padrão (sem parâmetros).
        /// </summary>
        private async void AbrirResultados_Click(object sender, RoutedEventArgs e)
        {
            // Preferir resolver via DI para manter consistência e permitir interceptações/mocks se necessário.
            var resultadosWindow = serviceProvider.GetRequiredService<ResultadosOtimizacaoWindow>();

            resultadosWindow.Owner = this;
            resultadosWindow.Show();

            // Usa o overload sem parâmetros (compat wrapper na janela de resultados).
            await resultadosWindow.CarregarDadosDoJobAsync();
        }

        // ===========================
        // Métodos privados
        // ===========================

        /// <summary>
        /// Reage à conclusão da execução do GAMS abrindo a janela de resultados
        /// e carregando o job pelo caminho da pasta retornado.
        /// </summary>
        /// <param name="result">Resultado de execução retornado pelo pipeline do GAMS.</param>
        private void OnOptimizationCompleted(GamsExecutionResult result)
        {
            // // Regra: quando há um job específico, passamos o caminho para carregar diretamente.
            var resultadosWindow = serviceProvider.GetRequiredService<ResultadosOtimizacaoWindow>();

            // Define Owner antes de exibir
            resultadosWindow.Owner = this;

            // Usa o overload com 1 argumento (string). A janela decide se é caminho/arquivo ou conteúdo.
            resultadosWindow.CarregarDadosDoJob(result.CaminhoPastaJob);

            resultadosWindow.Show();
        }
    }
}
