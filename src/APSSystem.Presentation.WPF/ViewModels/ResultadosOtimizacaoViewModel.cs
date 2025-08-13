using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using APSSystem.Application.DTOs;
using APSSystem.Application.UseCases.AnalisarResultadoGams;
using CommunityToolkit.Mvvm.Input; // Usando o pacote padrão da indústria
using MediatR;

namespace APSSystem.Presentation.WPF.ViewModels
{
    public sealed class ResultadosOtimizacaoViewModel : ViewModelBase
    {
        private readonly IMediator _mediator;

        private double _orderFulfillmentPercentage;
        public double OrderFulfillmentPercentage
        {
            get => _orderFulfillmentPercentage;
            private set { _orderFulfillmentPercentage = value; OnPropertyChanged(); }
        }

        private double _averageWastePercentage;
        public double AverageWastePercentage
        {
            get => _averageWastePercentage;
            private set { _averageWastePercentage = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ProducaoDto> Itens { get; } = new();
        public ObservableCollection<PlanoProducaoItem> PlanoProducao { get; } = new();
        public ObservableCollection<PlanoClienteItem> PlanoCliente { get; } = new();
        public IAsyncRelayCommand CarregarArquivoCommand { get; }

        public ResultadosOtimizacaoViewModel(IMediator mediator)
        {
            this._mediator = mediator;
            CarregarArquivoCommand = new AsyncRelayCommand(CarregarArquivoAsync);
        }

        public async Task CarregarArquivoAsync()
        {
            var caminho = SelecionarArquivo();
            if (string.IsNullOrWhiteSpace(caminho) || !File.Exists(caminho))
                return;

            var conteudo = await File.ReadAllTextAsync(caminho);
            var cmd = new AnalisarResultadoGamsCommand(conteudo);
            var resultado = await _mediator.Send(cmd);

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                Itens.Clear();
                foreach (var item in resultado) Itens.Add(item);

                var porLinha = resultado
                    .GroupBy(x => x.Linha ?? string.Empty)
                    .Select(g => new PlanoProducaoItem
                    {
                        Linha = g.Key,
                        QuantidadeTotal = g.Sum(x => x.Quantidade)
                    }).OrderByDescending(x => x.QuantidadeTotal);

                PlanoProducao.Clear();
                foreach (var p in porLinha) PlanoProducao.Add(p);

                var porProduto = resultado
                    .GroupBy(x => x.Produto ?? string.Empty)
                    .Select(g => new PlanoClienteItem
                    {
                        ClienteOuProduto = g.Key,
                        QuantidadeTotal = g.Sum(x => x.Quantidade)
                    }).OrderByDescending(x => x.QuantidadeTotal);

                PlanoCliente.Clear();
                foreach (var p in porProduto) PlanoCliente.Add(p);

                var total = resultado.Count;
                var atendidos = resultado.Count(x => x.Quantidade > 0);
                OrderFulfillmentPercentage = total == 0 ? 0 : Math.Round((double)atendidos / total * 100.0, 2);
                AverageWastePercentage = 0;
            });
        }

        private static string SelecionarArquivo()
        {
            var ofd = new Microsoft.Win32.OpenFileDialog { Filter = "GAMS Output|*.csv;*.txt;*.lst|All Files|*.*" };
            return ofd.ShowDialog() == true ? ofd.FileName : string.Empty;
        }
    }
}