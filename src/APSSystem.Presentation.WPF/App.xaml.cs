using System;
using System.Windows;
using APSSystem.Application.Interfaces;
using APSSystem.Infrastructure.GamsIntegration;
using APSSystem.Presentation.WPF.ViewModels;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace APSSystem.Presentation.WPF
{
    public partial class App : System.Windows.Application
    {
        private ServiceProvider? serviceProvider;

        /// <summary>
        /// Provedor de serviços do app para Injeção de Dependência.
        /// </summary>
        public IServiceProvider Services => serviceProvider!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();

            // Registra handlers do MediatR a partir do assembly da camada de Application.
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(
                    typeof(APSSystem.Application.UseCases.AnalisarResultadoGams.AnalisarResultadoGamsCommand).Assembly);
            });

            // Registra implementações da Infrastructure
            services.AddSingleton<IGamsOutputParser, GamsOutputParser>();

            // Registra os ViewModels
            services.AddTransient<ResultadosOtimizacaoViewModel>();

            serviceProvider = services.BuildServiceProvider();

            // Abre a janela principal ao iniciar
            //var mainWindow = new ResultadosOtimizacaoWindow();
            //mainWindow.Show();
        }
    }
}