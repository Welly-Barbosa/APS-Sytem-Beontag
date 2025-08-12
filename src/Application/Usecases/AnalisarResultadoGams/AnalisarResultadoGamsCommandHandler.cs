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
    private readonly IOrdemClienteRepository _ordemClienteRepo;
    private readonly ICalculadoraDePerdaService _calculadoraDePerda;

    public AnalisarResultadoGamsCommandHandler(IGamsOutputParser gamsParser, IOrdemClienteRepository ordemClienteRepo, ICalculadoraDePerdaService calculadoraDePerda)
    {
        _gamsParser = gamsParser;
        _ordemClienteRepo = ordemClienteRepo;
        _calculadoraDePerda = calculadoraDePerda;
    }

    public async Task<ResultadoGamsAnalisado> Handle(AnalisarResultadoGamsCommand request, CancellationToken cancellationToken)
    {
        var dadosGams = await _gamsParser.ParseAsync(request.CaminhoPastaJob);
        var ordensOriginais = (await _ordemClienteRepo.GetAllAsync()).ToList();
        var perdaPorPadrao = _calculadoraDePerda.CalcularPerdaPorPadrao(dadosGams.PlanoDeProducao, dadosGams.ComposicaoDosPadroes);

        var planoClienteDetalhado = new List<ItemDePlanoDetalhado>();
        foreach (var status in dadosGams.StatusDasEntregas)
        {
            var ordensCorrespondentes = ordensOriginais.Where(o =>
                o.ItemRequisitado.PN_Generico == status.PN_Base &&
                o.ItemRequisitado.Largura == status.LarguraProduto &&
                o.ItemRequisitado.Comprimento == status.CompProduto &&
                o.DataEntrega.Date == status.DataEntregaRequerida.Date
            ).ToList();

            foreach (var ordem in ordensCorrespondentes)
            {
                planoClienteDetalhado.Add(new ItemDePlanoDetalhado(
                    ordem.NumeroOrdem,
                    status.PN_Base,
                    status.LarguraProduto,
                    status.CompProduto,
                    status.DataEntregaRequerida,
                    ordem.Quantidade,
                    status.DataProducaoReal,
                    status.DiasDesvio,
                    TraduzirStatus(status.StatusEntrega)
                ));
            }
        }

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

        var totalOrdens = planoClienteDetalhado.Count;
        var ordensNoPrazo = planoClienteDetalhado.Count(p => p.StatusEntrega == "On Time");
        var fulfillment = totalOrdens > 0 ? (decimal)ordensNoPrazo / totalOrdens : 0;
        var perdaMedia = perdaPorPadrao.Values.Any() ? perdaPorPadrao.Values.Average() : 0;

        return new ResultadoGamsAnalisado
        {
            // CORREÇÃO: Usando o nome correto da propriedade: PlanoCliente
            PlanoCliente = planoClienteDetalhado.OrderBy(p => p.DataEntregaRequerida).ToList(),
            PlanoProducao = planoProducao,
            OrderFulfillmentPercentage = fulfillment * 100,
            AverageWastePercentage = perdaMedia * 100,
        };
    }

    private string TraduzirStatus(int statusGams) => statusGams switch
    {
        0 => "On Time",
        -1 => "Late / Not Planned",
        _ => "Unknown"
    };
}