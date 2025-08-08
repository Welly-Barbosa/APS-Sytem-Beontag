using APSSystem.Application.Interfaces;
using APSSystem.Application.Services;
using APSSystem.Application.UseCases.AnalisarResultadoGams;
using APSSystem.Application.UseCases.GerarArquivoGams;
using APSSystem.Core.Interfaces;
using APSSystem.Core.Services;
using APSSystem.Core.ValueObjects;
using APSSystem.Infrastructure.GamsIntegration;
using APSSystem.Infrastructure.Persistence.ExcelRepositories;
using APSSystem.Infrastructure.Persistence.InMemoryRepositories;
using APSSystem.Infrastructure.Services;
using APSSystem.Presentation.WPF.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;

namespace APSSystem.Presentation.WPF;

public partial class App : System.Windows.Application
{
    private IHost _host;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                ConfigureDependencies(services, context.Configuration);
            })
            .Build();
    }

    private void ConfigureDependencies(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(configuration);
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(GerarArquivoGamsCommandHandler).Assembly));

        services.AddSingleton<IOrdemClienteRepository, ExcelOrdemClienteRepository>();
        services.AddSingleton<IItemDeInventarioRepository, ExcelItemDeInventarioRepository>();
        services.AddSingleton<IRecursoRepository, ExcelRecursoRepository>();
        services.AddSingleton<IProdutoRepository, ExcelProdutoRepository>();
        services.AddSingleton<ICalendarioRepository, InMemoryCalendarioRepository>();
        services.AddSingleton<INecessidadeDeProducaoRepository, InMemoryNecessidadeDeProducaoRepository>();

        services.AddSingleton<IScenarioService, ScenarioService>();
        services.AddSingleton<IExcelDataService, ExcelDataService>();
        services.AddSingleton<IGamsFileWriter, GamsFileWriter>();
        services.AddSingleton<IGamsExecutionService, GamsExecutionService>();
        services.AddTransient<AlocacaoInventarioService>();
        services.AddTransient<ICalculadoraDeCargaService, CalculadoraDeCargaService>();
        services.AddTransient<ICalculadoraDePerdaService, CalculadoraDePerdaService>();

        services.AddSingleton(new ParametrosDeCalculoDeCarga(
            LarguraBobinaMae: 78.74m, FatorDePerda: 1.05m,
            TempoProcessamentoBobina10k: 60, TempoProcessamentoBobina15k: 90,
            TempoSetupPorBobina: 15, LarguraBobinaMaeGams: 78.74m));

        services.AddSingleton<MainWindow>();
        services.AddTransient<ResultadosOtimizacaoWindow>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<ResultadosOtimizacaoViewModel>();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        using (_host) { await _host.StopAsync(); }
        base.OnExit(e);
    }
}