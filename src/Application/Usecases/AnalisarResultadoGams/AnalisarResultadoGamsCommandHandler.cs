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

        // Agrupa as composições por padrão de corte, formatando a string sem o PN_Base.
        var composicaoAgrupada = dadosGams.ComposicaoDosPadroes
            .GroupBy(c => c.PadraoCorte)
            .ToDictionary(g => g.Key, g => string.Join(" / ", g.Select(item => $"{item.QtdPorBobinaMae:F0} x {item.LarguraProduto}")));

        var planoProducao = dadosGams.PlanoDeProducao
            .Select(plano =>
            {
                // Para evitar múltiplas buscas, obtemos a primeira referência da composição para este padrão.
                // O produto e o comprimento são os mesmos para todas as saídas de um mesmo padrão de corte.
                var infoComposicao = dadosGams.ComposicaoDosPadroes.FirstOrDefault(c => c.PadraoCorte == plano.PadraoCorte);

                return new ItemOrdemProducao(
                    DataProducao: plano.DataProducao,
                    Maquina: plano.Maquina,
                    JobNumber: plano.PadraoCorte,
                    Product: infoComposicao?.PN_Base ?? "N/A", // Popula o novo campo 'Product'
                    Length: infoComposicao?.CompProduto ?? 0, // Usa a mesma fonte para garantir consistência
                    QtdBobinasMae: (int)plano.QtdBobinasMae,
                    Composition: composicaoAgrupada.GetValueOrDefault(plano.PadraoCorte, ""),
                    WastePercentage: perdaPorPadrao.GetValueOrDefault(plano.PadraoCorte)
                );
            }).ToList();

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

        // Cálculo do Order Fulfillment (lógica mantida).
        var fulfillment = planoCliente.Any() ? (decimal)(planoCliente.Count(p => p.Status == "On Time" || p.Status == "Antecipated")) / planoCliente.Count : 0;

        // --- INÍCIO DA ALTERAÇÃO: Cálculo da Perda Média Ponderada ---

        // Calcula a soma total de bobinas-mãe utilizadas no plano.
        var totalBobinasMae = planoProducao.Sum(p => p.QtdBobinasMae ?? 0);

        // Calcula o somatório do produto da perda pelo número de bobinas para cada ordem de produção.
        var somatorioPonderadoDasPerdas = planoProducao.Sum(p => p.WastePercentage * (p.QtdBobinasMae ?? 0));

        // Calcula a média ponderada, garantindo a prevenção de divisão por zero.
        var perdaMedia = totalBobinasMae > 0
            ? somatorioPonderadoDasPerdas / totalBobinasMae
            : 0;

        // --- FIM DA ALTERAÇÃO ---

        return new ResultadoGamsAnalisado
        {
            PlanoCliente = planoCliente,
            PlanoProducao = planoProducao,
            OrderFulfillmentPercentage = fulfillment,
            AverageWastePercentage = perdaMedia,
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