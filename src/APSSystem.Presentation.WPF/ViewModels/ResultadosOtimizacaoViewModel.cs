using APSSystem.Application.DTOs;
using APSSystem.Application.UseCases.AnalisarResultadoGams;
using APSSystem.Presentation.WPF.Commands; // AsyncRelayCommand
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Measure;                  // DataLabelsPosition
using LiveChartsCore.SkiaSharpView;            // Axis, StackedRowSeries
using MediatR;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace APSSystem.Presentation.WPF.ViewModels
{
    /// <summary>
    /// ViewModel que orquestra o carregamento, agregação e exibição dos resultados
    /// de otimização provenientes do GAMS.
    /// </summary>
    public sealed class ResultadosOtimizacaoViewModel : INotifyPropertyChanged
    {
        // ===========================
        // Campos privados
        // ===========================
        private readonly IMediator mediator;
        private string statusMessage = "Pronto.";
        private Axis[] xAxesGantt = Array.Empty<Axis>();
        private Axis[] yAxesGantt = Array.Empty<Axis>();

        // ===========================
        // Propriedades públicas (dados)
        // ===========================

        /// <summary>
        /// Itens detalhados retornados pelo parser (exibição em grid).
        /// </summary>
        public ObservableCollection<ProducaoDto> Itens { get; } = new();

        /// <summary>
        /// Série agregada por linha de produção (Quantidade total por Linha).
        /// </summary>
        public ObservableCollection<PlanoProducaoItem> PlanoProducao { get; } = new();

        /// <summary>
        /// Série agregada por cliente (enquanto não existir Cliente no DTO,
        /// usamos Produto como proxy para fins de visualização).
        /// </summary>
        public ObservableCollection<PlanoClienteItem> PlanoCliente { get; } = new();

        /// <summary>
        /// Série para o gráfico de Gantt (implementado com barras empilhadas).
        /// </summary>
        public ObservableCollection<ISeries> SeriesGantt { get; } = new();

        /// <summary>
        /// Eixo X para o Gantt (escala temporal formatada).
        /// </summary>
        public Axis[] XAxesGantt
        {
            get => xAxesGantt;
            private set
            {
                xAxesGantt = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Eixo Y para o Gantt (categorias por atividade/linha-produto).
        /// </summary>
        public Axis[] YAxesGantt
        {
            get => yAxesGantt;
            private set
            {
                yAxesGantt = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Mensagem de status para feedback na UI.
        /// </summary>
        public string StatusMessage
        {
            get => statusMessage;
            private set
            {
                statusMessage = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Percentual de atendimento (proxy): % de registros com Quantidade &gt; 0.
        /// </summary>
        public double OrderFulfillmentPercentage { get; private set; }

        /// <summary>
        /// Percentual médio de desperdício (placeholder; 0 até existir no DTO).
        /// </summary>
        public double AverageWastePercentage { get; private set; }

        /// <summary>
        /// Comando para abrir/ler um arquivo de saída do GAMS e carregar seus dados.
        /// </summary>
        public ICommand CarregarArquivoCommand { get; }

        // ===========================
        // Construtores
        // ===========================

        /// <summary>
        /// Cria uma instância do ViewModel com injeção do IMediator.
        /// </summary>
        /// <param name="mediator">Instância de IMediator para acionar o caso de uso.</param>
        public ResultadosOtimizacaoViewModel(IMediator mediator)
        {
            // Guard Clause: dependência obrigatória
            this.mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

            // Inicializa comandos
            CarregarArquivoCommand = new AsyncRelayCommand(CarregarArquivoAsync);
        }

        // ===========================
        // Métodos públicos
        // ===========================

        /// <summary>
        /// Compatibilidade com code-behind legado que chama CarregarResultados() - dispara assíncrono.
        /// </summary>
        public void CarregarResultados() => _ = CarregarArquivoAsync();

        /// <summary>
        /// Compatibilidade assíncrona para uso com await no code-behind.
        /// </summary>
        public Task CarregarResultadosAsync() => CarregarArquivoAsync();

        /// <summary>
        /// Seleciona um arquivo (via UI) e carrega os dados do GAMS.
        /// </summary>
        public async Task CarregarArquivoAsync()
        {
            StatusMessage = "Selecione o arquivo de saída do GAMS...";
            var caminho = SelecionarArquivo();

            if (string.IsNullOrWhiteSpace(caminho) || !File.Exists(caminho))
            {
                StatusMessage = "Operação cancelada.";
                return;
            }

            await CarregarArquivoAsync(caminho).ConfigureAwait(false);
        }

        /// <summary>
        /// Carrega o conteúdo a partir de um caminho de arquivo específico.
        /// </summary>
        /// <param name="caminho">Caminho do arquivo de saída do GAMS.</param>
        public async Task CarregarArquivoAsync(string caminho)
        {
            if (string.IsNullOrWhiteSpace(caminho) || !File.Exists(caminho))
            {
                StatusMessage = "Caminho inválido.";
                return;
            }

            StatusMessage = "Lendo arquivo...";
            var conteudo = await File.ReadAllTextAsync(caminho).ConfigureAwait(false);
            await ProcessarConteudoAsync(conteudo).ConfigureAwait(false);
        }

        /// <summary>
        /// Carrega os dados a partir de conteúdo textual já lido.
        /// </summary>
        /// <param name="conteudo">Conteúdo bruto do arquivo de saída do GAMS.</param>
        public Task CarregarConteudoAsync(string conteudo)
            => ProcessarConteudoAsync(conteudo);

        // ===========================
        // Métodos privados
        // ===========================

        /// <summary>
        /// Processa o conteúdo do GAMS (envia ao caso de uso, preenche coleções, KPIs e gráfico).
        /// </summary>
        /// <param name="conteudo">Conteúdo bruto do arquivo de saída do GAMS.</param>
        private async Task ProcessarConteudoAsync(string conteudo)
        {
            StatusMessage = "Processando dados...";
            var cmd = new AnalisarResultadoGamsCommand(conteudo);
            var resultado = await mediator.Send(cmd).ConfigureAwait(false);

            // 1) Grid - limpa e carrega
            Itens.Clear();
            foreach (var item in resultado)
                Itens.Add(item);

            // 2) PlanoProducao: soma por Linha
            PlanoProducao.Clear();

            // // Agrupamos por Linha e somamos Quantidade
            var porLinha = resultado
                .GroupBy(x => x.Linha ?? string.Empty)
                .Select(g => new PlanoProducaoItem
                {
                    Linha = g.Key,
                    QuantidadeTotal = g.Sum(x => x.Quantidade)
                })
                .OrderByDescending(x => x.QuantidadeTotal);

            foreach (var p in porLinha)
                PlanoProducao.Add(p);

            // 3) PlanoCliente (proxy por Produto)
            PlanoCliente.Clear();

            // // Agrupamos por Produto e somamos Quantidade (até existir Cliente real no DTO)
            var porProduto = resultado
                .GroupBy(x => x.Produto ?? string.Empty)
                .Select(g => new PlanoClienteItem
                {
                    ClienteOuProduto = g.Key,
                    QuantidadeTotal = g.Sum(x => x.Quantidade)
                })
                .OrderByDescending(x => x.QuantidadeTotal);

            foreach (var p in porProduto)
                PlanoCliente.Add(p);

            // 4) KPIs (placeholders robustos)
            var total = resultado.Count;
            var atendidos = resultado.Count(x => x.Quantidade > 0);
            OrderFulfillmentPercentage = total == 0 ? 0 : Math.Round((double)atendidos / total * 100.0, 2);
            AverageWastePercentage = 0;

            // 5) Monta o Gantt (se houver janelas de início/fim)
            MontarGantt(resultado);

            StatusMessage = "Concluído.";
        }

        /// <summary>
        /// Monta o gráfico de Gantt usando duas séries empilhadas:
        /// uma de offset (invisível) e outra de duração (visível).
        /// </summary>
        /// <param name="resultado">Lista de itens de produção.</param>
        private void MontarGantt(List<ProducaoDto> resultado)
        {
            // // Seleciona apenas itens com janela válida
            var comJanela = resultado
                .Where(i => i.Inicio.HasValue && i.Fim.HasValue && i.Fim.Value > i.Inicio.Value)
                .ToList();

            SeriesGantt.Clear();

            if (comJanela.Count == 0)
            {
                // Eixos vazios para limpar bindings anteriores
                XAxesGantt = Array.Empty<Axis>();
                YAxesGantt = Array.Empty<Axis>();
                return;
            }

            // Base temporal = menor início
            var baseTime = comJanela.Min(i => i.Inicio!.Value);

            // Categorias (uma por tarefa/linha-produto)
            var categorias = comJanela
                .Select(i => $"{(i.Linha ?? "-")}: {(i.Produto ?? "-")}")
                .ToArray();

            // Offset (horas desde a base até o início) e Duração (horas entre início e fim)
            var offsets = comJanela
                .Select(i => (i.Inicio!.Value - baseTime).TotalHours)
                .ToArray();

            var duracoes = comJanela
                .Select(i => (i.Fim!.Value - i.Inicio!.Value).TotalHours)
                .ToArray();

            // Série 1: OFFSET (invisível) - só para empurrar a barra até o início
            var serieOffset = new StackedRowSeries<double>
            {
                Values = offsets,
                Name = "Offset",
                IsHoverable = false,
                // Invisível: sem Fill/Stroke
                Fill = null,
                Stroke = null
            };

            // Série 2: DURAÇÃO (visível) — DataLabels usa Coordinate.PrimaryValue (API atual do LiveCharts v2)
            var serieDuracao = new StackedRowSeries<double>
            {
                Values = duracoes,
                Name = "Duração",
                DataLabelsPosition = DataLabelsPosition.Middle,
                // Substitui o uso obsoleto de point.PrimaryValue
                DataLabelsFormatter = point =>
                {
                    var hours = point.Coordinate.PrimaryValue;
                    // // Robustez contra valores inválidos
                    if (double.IsNaN(hours) || double.IsInfinity(hours) || hours < 0) return string.Empty;

                    var ts = TimeSpan.FromHours(hours);
                    return ts.ToString(@"hh\:mm");
                }
            };

            SeriesGantt.Add(serieOffset);
            SeriesGantt.Add(serieDuracao);

            // Eixo X em tempo (formatando a partir da base)
            XAxesGantt = new[]
            {
                new Axis
                {
                    Labeler = value => baseTime.AddHours(value).ToString("dd/MM HH:mm"),
                    UnitWidth = 1, // 1 hora
                    MinLimit = 0
                }
            };

            // Eixo Y com categorias
            YAxesGantt = new[]
            {
                new Axis
                {
                    Labels = categorias
                }
            };
        }

        /// <summary>
        /// Encapsula a seleção do arquivo (substitua por OpenFileDialog na View).
        /// </summary>
        private static string SelecionarArquivo()
        {
            // Exemplo placeholder: retornar vazio para não abrir nada por padrão.
            // Em produção, use:
            // var ofd = new Microsoft.Win32.OpenFileDialog { Filter = "Saída GAMS|*.csv;*.txt;*.lst|Todos|*.*" };
            // return ofd.ShowDialog() == true ? ofd.FileName : string.Empty;
            return string.Empty;
        }

        // ===========================
        // INotifyPropertyChanged
        // ===========================

        /// <summary>
        /// Evento disparado quando uma propriedade pública é alterada.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Notifica a UI sobre alterações em propriedades.
        /// </summary>
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        // ===========================
        // Tipos aninhados (evitam colisões com classes homônimas em outros arquivos)
        // ===========================

        /// <summary>
        /// Item agregado por linha de produção.
        /// </summary>
        public sealed class PlanoProducaoItem
        {
            /// <summary>
            /// Identificador/nome da linha de produção.
            /// </summary>
            public string Linha { get; set; } = string.Empty;

            /// <summary>
            /// Quantidade total planejada/produzida na linha.
            /// </summary>
            public double QuantidadeTotal { get; set; }
        }

        /// <summary>
        /// Item agregado por cliente (ou produto como proxy).
        /// </summary>
        public sealed class PlanoClienteItem
        {
            /// <summary>
            /// Nome do cliente ou, provisoriamente, do produto.
            /// </summary>
            public string ClienteOuProduto { get; set; } = string.Empty;

            /// <summary>
            /// Quantidade total atribuída ao cliente/produto.
            /// </summary>
            public double QuantidadeTotal { get; set; }
        }
    }
}
