using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using APSSystem.Presentation.WPF.ViewModels;
using WpfApp = System.Windows.Application;

namespace APSSystem.Presentation.WPF
{
    /// <summary>
    /// Janela de visualização dos resultados da otimização.
    /// </summary>
    public partial class ResultadosOtimizacaoWindow : Window
    {
        public ResultadosOtimizacaoWindow()
        {
            InitializeComponent();

            // Resolve o ViewModel via DI e define DataContext
            var sp = ((App)WpfApp.Current).Services;
            DataContext = sp.GetRequiredService<ResultadosOtimizacaoViewModel>();

            // (Opcional) carregar automaticamente ao exibir a janela
            // Loaded += async (_, __) => await CarregarDadosDoJobAsync();
        }

        /// <summary>
        /// Compatibilidade: versão síncrona SEM parâmetros (chamada pela MainWindow).
        /// </summary>
        public void CarregarDadosDoJob()
        {
            _ = CarregarDadosDoJobAsync(null);
        }

        /// <summary>
        /// Compatibilidade: versão assíncrona SEM parâmetros (chamada pela MainWindow).
        /// </summary>
        public Task CarregarDadosDoJobAsync()
        {
            return CarregarDadosDoJobAsync(null);
        }

        /// <summary>
        /// Compatibilidade: permite chamar com 1 argumento.
        /// Se for string e caminho válido, carrega por arquivo; se for string não-arquivo, trata como conteúdo.
        /// Outros tipos caem no fluxo padrão (selecionar arquivo).
        /// </summary>
        public void CarregarDadosDoJob(object? arg)
        {
            _ = CarregarDadosDoJobAsync(arg);
        }

        /// <summary>
        /// Versão assíncrona que aceita argumento opcional.
        /// </summary>
        public async Task CarregarDadosDoJobAsync(object? arg)
        {
            if (DataContext is not ResultadosOtimizacaoViewModel vm)
                return;

            if (arg is string s)
            {
                if (File.Exists(s))
                {
                    await vm.CarregarArquivoAsync(s).ConfigureAwait(false);
                    return;
                }

                await vm.CarregarConteudoAsync(s).ConfigureAwait(false);
                return;
            }

            // Sem argumento útil: segue o fluxo padrão (OpenFileDialog do VM)
            await vm.CarregarArquivoAsync().ConfigureAwait(false);
        }
    }
}
