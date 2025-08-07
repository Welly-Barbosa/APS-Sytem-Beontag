using APSSystem.Application.Interfaces;
using APSSystem.Application.Services;
using APSSystem.Application.UseCases.GerarArquivoGams;
using APSSystem.Core.Interfaces;
using APSSystem.Core.Services;
using APSSystem.Core.ValueObjects;
using APSSystem.Infrastructure.GamsIntegration;
using APSSystem.Infrastructure.Persistence.ExcelRepositories;
using APSSystem.Infrastructure.Persistence.InMemoryRepositories;
using APSSystem.Infrastructure.Services;
using APSSystem.Presentation.WPF.ViewModels;
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
            .ConfigureServices((context, services) =>
            {
                ConfigureDependencies(services);
            })
            .Build();
    }

    private void ConfigureDependencies(IServiceCollection services)
    {
        // MediatR para orquestração de casos de uso
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(GerarArquivoGamsCommandHandler).Assembly));

        // --- Repositórios ---
        // Repositórios que leem de planilhas Excel
        services.AddSingleton<IOrdemClienteRepository, ExcelOrdemClienteRepository>();
        services.AddSingleton<IItemDeInventarioRepository, ExcelItemDeInventarioRepository>();
        services.AddSingleton<IRecursoRepository, ExcelRecursoRepository>();
        services.AddSingleton<IProdutoRepository, ExcelProdutoRepository>();

        // Repositórios que usam dados em memória
        services.AddSingleton<ICalendarioRepository, InMemoryCalendarioRepository>();

        // --- O REGISTRO QUE FALTAVA ---
        // Adiciona a implementação em memória para o repositório de Necessidade de Produção
        services.AddSingleton<INecessidadeDeProducaoRepository, InMemoryNecessidadeDeProducaoRepository>();


        // --- Serviços ---
        // Serviços de negócio e de infraestrutura
        services.AddSingleton<IScenarioService, ScenarioService>();
        services.AddSingleton<IExcelDataService, ExcelDataService>();
        services.AddSingleton<IGamsFileWriter, GamsFileWriter>();
        services.AddTransient<AlocacaoInventarioService>();
        services.AddSingleton<IGamsExecutionService, GamsExecutionService>();

        // Parâmetros de Cálculo
        services.AddSingleton(new ParametrosDeCalculoDeCarga(
            LarguraBobinaMae: 78.74m, FatorDePerda: 1.05m,
            TempoProcessamentoBobina10k: 60, TempoProcessamentoBobina15k: 90,
            TempoSetupPorBobina: 15));
        services.AddTransient<ICalculadoraDeCargaService, CalculadoraDeCargaService>();

        // --- Camada de UI (Views e ViewModels) ---
        services.AddSingleton<MainWindow>();
        services.AddTransient<DashboardViewModel>();
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