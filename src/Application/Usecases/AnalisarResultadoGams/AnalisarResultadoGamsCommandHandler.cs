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

    public AnalisarResultadoGamsCommandHandler(IGamsOutputParser gamsParser, ICalculadoraDePerdaService calculadoraDePerda)
    {
        _gamsParser = gamsParser;
        _calculadoraDePerda = calculadoraDePerda;
    }

    public async Task<ResultadoGamsAnalisado> Handle(AnalisarResultadoGamsCommand request, CancellationToken cancellationToken)
    {
        // 1. Usa o parser para ler os dados brutos dos arquivos de resultado
        var dadosGams = await _gamsParser.ParseAsync(request.CaminhoPastaJob);

        // 2. Implementa a nova lógica: Calcular os KPIs globais
        var perdaPorPadrao = _calculadoraDePerda.CalcularPerdaPorPadrao(dadosGams.PlanoDeProducao, dadosGams.ComposicaoDosPadroes);

        // 3. Monta a lista de Ordens de Produção para a tabela, juntando com a composição e a perda
        var planoProducao = (from plano in dadosGams.PlanoDeProducao
                             join comp in dadosGams.ComposicaoDosPadroes on plano.PadraoCorte equals comp.PadraoCorte into comps
                             let composicaoFormatada = string.Join(", ", comps.Select(c => $"{c.QtdPorBobinaMae}x {c.PN_Base}-{c.LarguraProduto}"))
                             select new ItemOrdemProducao(
                                 plano.DataProducao,
                                 plano.Maquina,
                                 plano.PadraoCorte,
                                 plano.QtdBobinasMae,
                                 composicaoFormatada,
                                 perdaPorPadrao.GetValueOrDefault(plano.PadraoCorte)
                             )).ToList();

        // 4. Monta a lista de status de Ordens de Cliente para a outra tabela
        var planoCliente = dadosGams.StatusDasEntregas
            .Select(status => new ItemDePlanoDetalhado(
                "N/A", // O NumeroOrdem original precisaria ser buscado do IOrdemClienteRepository
                status.PN_Base,
                status.LarguraProduto,
                status.CompProduto,
                status.DataEntregaRequerida,
                status.QtdDemandada,
                status.DataProducaoReal,
                status.DiasDesvio,
                TraduzirStatus(status.StatusEntrega)
            ))
            .OrderBy(p => p.DataEntregaRequerida)
            .ToList();

        // 5. Calcula os KPIs de alto nível
        var totalOrdens = planoCliente.Count;
        var ordensNoPrazo = planoCliente.Count(p => p.StatusEntrega == "On Time");
        var fulfillment = totalOrdens > 0 ? (decimal)ordensNoPrazo / totalOrdens : 0;
        var perdaMedia = perdaPorPadrao.Values.Any() ? perdaPorPadrao.Values.Average() : 0;

        return new ResultadoGamsAnalisado
        {
            PlanoCliente = planoCliente,
            PlanoProducao = planoProducao,
            OrderFulfillmentPercentage = fulfillment * 100, // em percentual
            AverageWastePercentage = perdaMedia * 100, // em percentual
            // O cálculo de Perda em Polegadas precisaria de mais detalhes, omitido por enquanto
        };
    }

    private string TraduzirStatus(int statusGams) => statusGams switch
    {
        0 => "On Time",
        -1 => "Late / Not Planned",
        _ => "Unknown"
    };
}