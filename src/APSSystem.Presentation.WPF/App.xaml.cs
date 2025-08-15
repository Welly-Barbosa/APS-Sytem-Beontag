using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using APSSystem.Application.UseCases.ObterDadosDashboard;

using APSSystem.Core.Interfaces;
using APSSystem.Core.Services;
using APSSystem.Core.ValueObjects;

using APSSystem.Application.Interfaces;
using APSSystem.Infrastructure.Services;
using APSSystem.Infrastructure.Persistence.ExcelRepositories;
using APSSystem.Infrastructure.Persistence.InMemoryRepositories;

using MediatR;

namespace APSSystem.Presentation.WPF
{
    /// <summary>
    ///     App WPF: compõe DI, Configuração e Logging sem depender de Microsoft.Extensions.Hosting.
    ///     Usamos ServiceCollection + ServiceProvider manualmente.
    /// </summary>
    public partial class App : System.Windows.Application
    {
        // Campos privados
        private ServiceProvider? serviceProvider;
        private IConfigurationRoot? configuration;

        // Propriedades públicas
        /// <summary>
        ///     Exposição da configuração global quando necessário fora de DI.
        /// </summary>
        public IConfiguration Configuration =>
            configuration ?? throw new InvalidOperationException("Configuração não inicializada.");

        /// <summary>
        ///     Construtor: conecta handlers globais de exceção.
        /// </summary>
        public App()
        {
            this.DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        /// <summary>
        ///     Inicialização: cria IConfiguration, DI e Logging; resolve e mostra a MainWindow.
        /// </summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            ShutdownMode = ShutdownMode.OnMainWindowClose;

            // 1) Configuração resiliente (tolera JSON inválido com fallback em memória)
            configuration = BuildConfigurationSafe();

            // 2) Monta a coleção de serviços (DI) e configura logging
            var services = new ServiceCollection();

            // Logging
            services.AddLogging(b =>
            {
                b.ClearProviders();
                b.AddConsole();
                b.AddDebug();
            });

            // Config -> adiciona IConfiguration direto ao container
            services.AddSingleton<IConfiguration>(configuration);

            // 3) Registra serviços da aplicação (MediatR, repositórios, VO, serviços, MainWindow)
            RegisterApplicationServices(configuration, services);

            // 4) Constrói o provider
            serviceProvider = services.BuildServiceProvider(validateScopes: true);

            // 5) Exibe a janela principal via DI
            var main = serviceProvider.GetRequiredService<MainWindow>();
            MainWindow = main;
            MainWindow.Show();

            base.OnStartup(e);
        }

        /// <summary>
        ///     Finalização ordenada: descarta o ServiceProvider.
        /// </summary>
        protected override void OnExit(ExitEventArgs e)
        {
            try { serviceProvider?.Dispose(); } catch { /* evitar exceção no shutdown */ }
            base.OnExit(e);
        }

        /// <summary>
        ///     Registro de serviços mantendo as escolhas do commit 9f4498c.
        /// </summary>
        private static void RegisterApplicationServices(IConfiguration configuration, IServiceCollection services)
        {
            // MediatR (varre o assembly Application para handlers)
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(ObterDadosDashboardQueryHandler).Assembly);
            });

            // Infra Excel / InMemory (estado mínimo do commit 9f4498c)
            services.AddTransient<IExcelDataService, ExcelDataService>();

            services.AddTransient<IRecursoRepository, ExcelRecursoRepository>();
            services.AddTransient<IItemDeInventarioRepository, ExcelItemDeInventarioRepository>();
            services.AddTransient<INecessidadeDeProducaoRepository, InMemoryNecessidadeDeProducaoRepository>();
            services.AddTransient<ICalendarioRepository, InMemoryCalendarioRepository>();

            services.AddTransient<IProdutoRepository, ExcelProdutoRepository>();
            services.AddTransient<IOrdemClienteRepository, ExcelOrdemClienteRepository>();

            // Parametrização (bind no Presentation; Core permanece puro)
            services.AddSingleton<ParametrosDeCalculoDeCarga>(sp =>
            {
                var cfg = configuration.GetSection("ParametrosDeCalculoDeCarga");
                var logger = sp.GetService<ILoggerFactory>()?.CreateLogger("DI");

                if (!cfg.Exists())
                {
                    logger?.LogWarning("Seção 'ParametrosDeCalculoDeCarga' não encontrada. Usando valores padrão.");
                    return new ParametrosDeCalculoDeCarga();
                }

                var vo = new ParametrosDeCalculoDeCarga();
                cfg.Bind(vo); // requer Microsoft.Extensions.Configuration.Binder neste projeto
                return vo;
            });

            // Serviços de domínio/aplicação
            services.AddScoped<ICalculadoraDeCargaService, CalculadoraDeCargaService>();

            // WPF: janela principal
            services.AddSingleton<MainWindow>();
        }

        /// <summary>
        ///     Constrói IConfiguration tolerando appsettings inválido; faz fallback para InMemory.
        /// </summary>
        private static IConfigurationRoot BuildConfigurationSafe()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddEnvironmentVariables(prefix: "APS_")
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            var env = Environment.GetEnvironmentVariable("APS_ENVIRONMENT")
                      ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                      ?? "Production";

            builder.AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true);

            try
            {
                return builder.Build();
            }
            catch (FormatException ex) when (ex.InnerException is JsonException jsonEx)
            {
                ShowJsonError("appsettings.json/appsettings.{ENV}.json", jsonEx);
                return new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Logging:LogLevel:Default"] = "Information"
                    })
                    .Build();
            }
            catch (JsonException ex)
            {
                ShowJsonError("appsettings.json/appsettings.{ENV}.json", ex);
                return new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>())
                    .Build();
            }
        }

        /// <summary>
        ///     Mensagem amigável quando o JSON de configuração está inválido.
        /// </summary>
        private static void ShowJsonError(string fileLabel, Exception ex)
        {
            MessageBox.Show(
                $"Não foi possível carregar a configuração de {fileLabel}.\n" +
                $"A aplicação continuará com configurações padrão.\n\nDetalhes: {ex.Message}",
                "Configuração inválida",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        /// <summary>
        ///     Handler: exceções não tratadas na UI (Dispatcher).
        /// </summary>
        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                serviceProvider?.GetService<ILogger<App>>()?
                    .LogError(e.Exception, "Exceção não tratada (Dispatcher).");
            }
            catch { /* evitar loop de exceções */ }

            MessageBox.Show($"Ocorreu um erro inesperado.\n\n{e.Exception.Message}",
                "Erro", MessageBoxButton.OK, MessageBoxImage.Error);

            e.Handled = true;
        }

        /// <summary>
        ///     Handler: exceções não observadas em Tasks.
        /// </summary>
        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            try
            {
                serviceProvider?.GetService<ILogger<App>>()?
                    .LogError(e.Exception, "Exceção não observada em Task.");
            }
            catch { }

            e.SetObserved();
        }

        /// <summary>
        ///     Handler: exceções não tratadas no AppDomain.
        /// </summary>
        private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                var logger = serviceProvider?.GetService<ILogger<App>>();
                if (e.ExceptionObject is Exception ex)
                    logger?.LogCritical(ex, "Exceção não tratada no AppDomain.");
                else
                    logger?.LogCritical("Exceção não tratada no AppDomain (objeto desconhecido).");
            }
            catch { }
        }
    }
}
