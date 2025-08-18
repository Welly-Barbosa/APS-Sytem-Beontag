using APSSystem.Application.Interfaces;
using APSSystem.Core.Interfaces;
using APSSystem.Core.Services;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace APSSystem.Application.UseCases.AnalisarResultadoGams;

public class AnalisarResultadoGamsCommandHandler : IRequestHandler<AnalisarResultadoGamsCommand, ResultadoGamsAnalisado>
{
    private readonly IGamsOutputParser _gamsParser;
    private readonly ICalculadoraDePerdaService _calculadoraDePerda;
    // Futuramente, injetaremos o repositório de NecessidadeDeProducao para a rastreabilidade

    public AnalisarResultadoGamsCommandHandler(IGamsOutputParser gamsParser, ICalculadoraDePerdaService calculadoraDePerda)
    {
        _gamsParser = gamsParser;
        _calculadoraDePerda = calculadoraDePerda;
    }

    public async Task<ResultadoGamsAnalisado> Handle(AnalisarResultadoGamsCommand request, CancellationToken cancellationToken)
    {
        var dadosGams = await _gamsParser.ParseAsync(request.CaminhoPastaJob);
        var perdaPorPadrao = _calculadoraDePerda.CalcularPerdaPorPadrao(dadosGams.PlanoDeProducao, dadosGams.ComposicaoDosPadroes);

        // Agrupa as composições por padrão de corte para a nova formatação
        var composicaoAgrupada = dadosGams.ComposicaoDosPadroes
            .GroupBy(c => c.PadraoCorte)
            .ToDictionary(g => g.Key, g => string.Join(" / ", g.Select(item => $"{item.QtdPorBobinaMae:F0}x{item.PN_Base}>{item.LarguraProduto}")));

        var planoProducao = dadosGams.PlanoDeProducao
            .Select(plano => new ItemOrdemProducao(
                DataProducao: plano.DataProducao,
                Maquina: plano.Maquina,
                QtdBobinasMae: (int)plano.QtdBobinasMae,
                JobNumber: plano.PadraoCorte, // Job # é o PadraoCorte
                Length: dadosGams.ComposicaoDosPadroes.FirstOrDefault(c => c.PadraoCorte == plano.PadraoCorte)?.CompProduto ?? 0,
                Composition: composicaoAgrupada.GetValueOrDefault(plano.PadraoCorte, ""),
                WastePercentage: perdaPorPadrao.GetValueOrDefault(plano.PadraoCorte)
            )).ToList();

        var planoCliente = dadosGams.StatusDasEntregas
            .Select(status => new ItemDePlanoDetalhado(
                CustomerOrderNumber: "N/A", // Placeholder para rastreabilidade
                Product: status.PN_Base,
                Length: status.CompProduto,
                CuttingWidth: status.LarguraProduto,
                RequiredDate: status.DataEntregaRequerida,
                RequiredQuantity: (int)status.QtdDemandada,
                PlannedDate: status.DataProducaoReal,
                Deviation: status.DiasDesvio,
                Status: TraduzirStatus(status.StatusEntrega, status.DiasDesvio)
            )).ToList();

        var fulfillment = planoCliente.Any() ? (decimal)(planoCliente.Count(p => p.Status == "On Time" || p.Status == "Antecipated")) / planoCliente.Count : 0;
        //var perdaMedia = perdaPorPadrao.Values.Any() ? perdaPorPadrao.Values.Average() : 0;

        // 1. Calcula o Order Fulfillment conforme a nova regra
        //var totalOrdens = planoCliente.Count;
        // Conta as ordens que não estão atrasadas (desvio <= 0) e foram planejadas (Status > 0)
        //var ordensAtendidasSemAtraso = planoCliente.Count(p => (p.Deviation ?? 0) <= 0 && p.Status != "Not Planned");
        //var fulfillment = totalOrdens > 0 ? (decimal)ordensAtendidasSemAtraso / totalOrdens : 0;

        // 2. Calcula a média de perda de material (lógica existente mantida por ser a correta para "Waste")
        var perdaMedia = perdaPorPadrao.Values.Any() ? perdaPorPadrao.Values.Average() : 0;

        // Cálculo do Order Fulfillment
        //var totalOrdensCliente = (decimal)planoCliente.Count;
        //var ordensComAtraso = (decimal)planoCliente.Count(p => p.Deviation.HasValue && p.Deviation > 0);
        //var fulfillment = totalOrdensCliente > 0
        //    ? (totalOrdensCliente - ordensComAtraso) / totalOrdensCliente
        //    : 0;

        // Cálculo do Average Waste %
        //var totalOrdensProducao = (decimal)planoProducao.Count;
        //var somaDasPerdas = planoProducao.Sum(p => p.WastePercentage);
        //var perdaMedia = totalOrdensProducao > 0
        //    ? somaDasPerdas / totalOrdensProducao
         //   : 0;


        return new ResultadoGamsAnalisado
        {
            PlanoCliente = planoCliente,
            PlanoProducao = planoProducao,
            OrderFulfillmentPercentage = fulfillment, //* 100,
            AverageWastePercentage = perdaMedia, // * 100,
        };
    }

    private string TraduzirStatus(int statusGams, int? diasDesvio)
    {
        if (statusGams > 0)
        {
            if (diasDesvio == 0 || diasDesvio == -1)
            {
                return "On Time";
            }
            if (diasDesvio < -1)
            {
                return "Antecipated";
            }
            // Se DiasDesvio for > 0, ainda é considerado "Late", mesmo que planejado.
            return "Late";
        }
        else
        {
            return "Not Planned"; // Simplificado para "Not Planned" quando não atendido.
        }
    }
}