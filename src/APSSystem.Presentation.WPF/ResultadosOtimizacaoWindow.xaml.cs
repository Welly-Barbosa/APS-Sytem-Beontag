using System.Windows;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using APSSystem.Presentation.WPF.ViewModels;
using WpfApp = System.Windows.Application;

namespace APSSystem.Presentation.WPF
{
    public partial class ResultadosOtimizacaoWindow
    {
        /// <summary>
        /// Compatibilidade com chamadas legadas a partir da MainWindow.
        /// Dispara o carregamento de dados sem bloquear a UI.
        /// </summary>
        public void CarregarDadosDoJob()
        {
            if (DataContext is ViewModels.ResultadosOtimizacaoViewModel vm)
            {
                _ = vm.CarregarResultadosAsync(); // fire-and-forget
            }
        }

        /// <summary>
        /// Versão assíncrona para cenários onde a MainWindow deseja await.
        /// </summary>
        public Task CarregarDadosDoJobAsync()
        {
            if (DataContext is ViewModels.ResultadosOtimizacaoViewModel vm)
            {
                return vm.CarregarResultadosAsync();
            }
            return Task.CompletedTask;
        }
    }
}
