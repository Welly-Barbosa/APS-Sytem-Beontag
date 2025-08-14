using System;
using System.Windows;
using APSSystem.Application.Interfaces;
using APSSystem.Infrastructure.GamsIntegration;
using APSSystem.Presentation.WPF.ViewModels;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using APSSystem.Application.Services;

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

            // >>> ADICIONE ESTE REGISTRO <<<
            services.AddTransient<IScenarioService, ScenarioService>(); // ajuste o tipo concreto

            // Registra os ViewModels
            services.AddTransient<MainWindow>();
            services.AddTransient<ResultadosOtimizacaoViewModel>();

            serviceProvider = services.BuildServiceProvider();

            // >>> Mostra a MainWindow via DI (sem StartupUri)
            var main = Services.GetRequiredService<MainWindow>();
            main.Show();
        }
    }
}