using APSSystem.Application.Interfaces;
using APSSystem.Core.Services;
using APSSystem.Core.ValueObjects;
using APSSystem.Infrastructure.GamsIntegration;
using APSSystem.Infrastructure.Persistence.ExcelRepositories;
using APSSystem.Infrastructure.Services;
using APSSystem.Presentation.WPF.ViewModels;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using WpfApp = System.Windows.Application;

namespace APSSystem.Presentation.WPF
{
    /// <summary>
    /// Ponto de entrada da aplicação WPF. Responsável por inicializar DI, configuração e abrir a MainWindow.
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private ServiceProvider? serviceProvider;

        /// <summary>
        /// Provedor de serviços do container DI.
        /// </summary>
        public IServiceProvider Services => serviceProvider!;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);

            services.AddMediatR(cfg =>
            {
                // Registra handlers a partir do assembly da camada de Application.
                cfg.RegisterServicesFromAssembly(typeof(APSSystem.Application.UseCases.AnalisarResultadoGams.AnalisarResultadoGamsCommand).Assembly);
            });
            
            // Registro por convenção
            RegisterApplicationServicesByConvention(services);
            RegisterRepositoriesByConvention(services);
            RegisterCoreServicesByConvention(services);
            
            
            // Registro explícito para serviços de infraestrutura que não seguem a convenção
            services.AddSingleton<IGamsOutputParser, GamsOutputParser>();
            services.AddTransient<APSSystem.Application.Interfaces.IExcelDataService, APSSystem.Infrastructure.Services.ExcelDataService>();
            services.AddTransient<APSSystem.Core.Interfaces.IRecursoRepository, APSSystem.Infrastructure.Persistence.ExcelRepositories.ExcelRecursoRepository>();
            services.AddTransient<APSSystem.Core.Interfaces.ICalendarioRepository, APSSystem.Infrastructure.Persistence.InMemoryRepositories.InMemoryCalendarioRepository>();
            services.AddTransient<APSSystem.Core.Interfaces.IItemDeInventarioRepository, APSSystem.Infrastructure.Persistence.ExcelRepositories.ExcelItemDeInventarioRepository>();
            services.AddTransient<APSSystem.Core.Interfaces.INecessidadeDeProducaoRepository, APSSystem.Infrastructure.Persistence.InMemoryRepositories.InMemoryNecessidadeDeProducaoRepository>();
            services.AddTransient<APSSystem.Core.Interfaces.IOrdemClienteRepository, APSSystem.Infrastructure.Persistence.InMemoryRepositories.InMemoryOrdemClienteRepository>();
            services.AddTransient<APSSystem.Core.Interfaces.IProdutoRepository, APSSystem.Infrastructure.Persistence.ExcelRepositories.ExcelProdutoRepository>();
            services.AddTransient<APSSystem.Core.Interfaces.IOrdemClienteRepository, APSSystem.Infrastructure.Persistence.ExcelRepositories.ExcelOrdemClienteRepository>();
            services.AddTransient<APSSystem.Core.ValueObjects.ParametrosDeCalculoDeCarga>();
            services.AddTransient<APSSystem.Core.Services.ICalculadoraDeCargaService>();
            // (Adicione outros registros explícitos aqui se necessário)

            // ViewModels e Windows
            services.AddTransient<ResultadosOtimizacaoViewModel>();
            services.AddSingleton<MainWindow>(); // MainWindow geralmente é Singleton
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<ResultadosOtimizacaoWindow>();

            // --- ALTERAÇÃO PRINCIPAL AQUI ---
            // Removemos o registro "hardcoded" e ativamos o registro a partir da configuração
            //TryRegisterParametrosDeCalculoDeCargaFromConfig(services, configuration);
            // Registro do Value Object a partir de appsettings
            //services.AddSingleton<APSSystem.Core.ValueObjects.ParametrosDeCalculoDeCarga>(sp =>
            //{
            //    var cfg = sp.GetRequiredService<IConfiguration>().GetSection("ParametrosDeCalculoDeCarga");
            //    return BindValueObjectFromConfig<APSSystem.Core.ValueObjects.ParametrosDeCalculoDeCarga>(cfg);
            //});

            services.AddSingleton<APSSystem.Core.Services.ICalculadoraDeCargaService,APSSystem.Core.Services.CalculadoraDeCargaService>();



            serviceProvider = services.BuildServiceProvider();

            // Pré-carrega planilhas (evita exceção no primeiro acesso do Dashboard)
            try
            {
                var cfg = Services.GetRequiredService<IConfiguration>();
                var basePath = cfg.GetSection("Excel").GetValue<string>("BasePath") ?? "Data";
                var arquivos = cfg.GetSection("Excel:Arquivos").Get<string[]>() ?? new[] { "Recursos.xlsx" };

                var excel = Services.GetRequiredService<IExcelDataService>();

                // Se sua interface já tiver um método de pré-carregamento, use-o.
                // Ex.: await excel.PrecarregarPlanilhasAsync(basePath, arquivos);

                // Caso NÃO exista na interface, veja o patch do item 3 para adicionar.
                await(excel as dynamic).PrecarregarPlanilhasAsync(basePath, arquivos);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"[Excel preload] {ex}");
                // Opcional: exibir uma mensagem amigável e seguir sem travar a UI
            }


            // Handlers globais de exceção
            this.DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            // Abre a MainWindow via DI
            try
            {
                var main = Services.GetRequiredService<MainWindow>();
                main.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Erro fatal ao inicializar a aplicação", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(-1);
            }
        }

        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Trace.WriteLine($"[UI ERROR] {e.Exception}");
            MessageBox.Show(e.Exception.ToString(), "Erro Inesperado na UI", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true; // Previne o crash da aplicação
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception ?? new Exception("Erro não-catalogado");
            Trace.WriteLine($"[FATAL ERROR] {ex} (IsTerminating={e.IsTerminating})");
        }

        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            Trace.WriteLine($"[TASK ERROR] {e.Exception}");
            e.SetObserved();
        }
        private static T BindValueObjectFromConfig<T>(IConfiguration section)
        {
            var t = typeof(T);
            var ctor = t.GetConstructors()
                .OrderByDescending(c => c.GetParameters().Length)
                .FirstOrDefault()
                ?? throw new InvalidOperationException($"Tipo {t.FullName} não possui construtor público.");

            var args = ctor.GetParameters().Select(p =>
            {
                var name = p.Name ?? string.Empty;

                if (p.ParameterType == typeof(decimal))
                {
                    var d = section.GetValue<decimal?>(name);
                    if (d.HasValue) return (object)d.Value;
                    var dbl = section.GetValue<double?>(name);
                    return (object)Convert.ToDecimal(dbl ?? 0.0);
                }
                if (p.ParameterType == typeof(double)) return (object)(section.GetValue<double?>(name) ?? 0.0);
                if (p.ParameterType == typeof(int)) return (object)(section.GetValue<int?>(name) ?? 0);
                if (p.ParameterType == typeof(bool)) return (object)(section.GetValue<bool?>(name) ?? false);
                if (p.ParameterType == typeof(string)) return (object)(section.GetValue<string?>(name) ?? string.Empty);

                var inst = Activator.CreateInstance(p.ParameterType);
                if (inst != null) return inst;

                throw new InvalidOperationException($"Não foi possível construir parâmetro '{name}' do tipo {p.ParameterType.Name}.");
            }).ToArray();

            return (T)Activator.CreateInstance(t, args)!;
        }

        /// Registra automaticamente serviços da camada Application:
        private static void RegisterApplicationServicesByConvention(IServiceCollection services)
        {
            // Anchor: qualquer tipo do assembly Application.Services (ajuste se necessário)
            var appServicesAssembly = typeof(APSSystem.Application.Services.ScenarioService).Assembly;

            // Todas as classes públicas concretas em Application.Services
            var impls = appServicesAssembly
                .GetTypes()
                .Where(t =>
                    t.IsClass &&
                    !t.IsAbstract &&
                    t.IsPublic &&
                    t.Namespace != null &&
                    t.Namespace.StartsWith("APSSystem.Application.Services", StringComparison.Ordinal))
                .ToArray();

            foreach (var impl in impls)
            {
                // Interface esperada: APSSystem.Application.Interfaces.I{NomeDaClasse}
                var iface = impl.GetInterfaces()
                    .FirstOrDefault(i =>
                        i.Namespace != null &&
                        i.Namespace.StartsWith("APSSystem.Application.Interfaces", StringComparison.Ordinal) &&
                        i.Name == $"I{impl.Name}");

                if (iface != null)
                {
                    services.AddTransient(iface, impl);
                }
            }
        }

        private static void RegisterRepositoriesByConvention(IServiceCollection services)
        {
            // Âncora para encontrar o Assembly da Infrastructure
            Assembly infraAssembly = typeof(GamsOutputParser).Assembly;

            var repositoryTypes = infraAssembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Repository"))
                .ToList();

            foreach (var type in repositoryTypes)
            {
                var interfaceType = type.GetInterfaces()
                    .FirstOrDefault(i => i.Name == $"I{type.Name}");

                if (interfaceType != null)
                {
                    // Registra como Singleton para manter o cache dos arquivos Excel
                    services.AddSingleton(interfaceType, type);
                }
            }
        }

        private static void RegisterCoreServicesByConvention(IServiceCollection services)
        {
            // Âncora para encontrar o Assembly do Core
            Assembly coreAssembly = typeof(ICalculadoraDeCargaService).Assembly;

            var serviceTypes = coreAssembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Service"))
                .ToList();

            foreach (var type in serviceTypes)
            {
                var interfaceType = type.GetInterfaces()
                    .FirstOrDefault(i => i.Name == $"I{type.Name}");

                if (interfaceType != null)
                {
                    // Serviços geralmente são Transient ou Scoped. Transient é mais seguro.
                    services.AddTransient(interfaceType, type);
                }
            }
        }
        /// <summary>
        /// Tenta registrar APSSystem.Core.ValueObjects.ParametrosDeCalculoDeCarga a partir da seção de configuração.
        /// </summary>
        private static void TryRegisterParametrosDeCalculoDeCargaFromConfig(IServiceCollection services, IConfiguration configuration)
        {
            var voType = Type.GetType("APSSystem.Core.ValueObjects.ParametrosDeCalculoDeCarga, APSSystem.Core");
            if (voType == null) return;

            // Registra uma fábrica que sabe como construir o objeto a partir da configuração
            services.AddSingleton(provider =>
            {
                var section = provider.GetRequiredService<IConfiguration>().GetSection("ParametrosDeCalculoDeCarga");
                return BindValueObjectFromConfig(section, voType);
            });
        }

        /// <summary>
        /// Cria uma instância de um Value Object a partir de uma seção de configuração.
        /// </summary>
        private static object BindValueObjectFromConfig(IConfiguration section, Type voType)
        {
            var ctor = voType.GetConstructors().FirstOrDefault()
                ?? throw new InvalidOperationException($"Tipo {voType.FullName} não possui construtor público.");

            var args = ctor.GetParameters().Select(p =>
            {
                var configValue = section[p.Name!]; // Busca a chave com o mesmo nome do parâmetro
                if (configValue is null)
                    throw new InvalidOperationException($"A chave de configuração '{p.Name}' não foi encontrada em 'ParametrosDeCalculoDeCarga'.");

                // Converte o valor da string para o tipo do parâmetro
                return Convert.ChangeType(configValue, p.ParameterType, CultureInfo.InvariantCulture);
            }).ToArray();

            return Activator.CreateInstance(voType, args)!;
        }


    }
}