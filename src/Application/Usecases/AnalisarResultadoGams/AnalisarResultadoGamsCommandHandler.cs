using APSSystem.Application.Interfaces;
using APSSystem.Core.Interfaces;
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

    public AnalisarResultadoGamsCommandHandler(IGamsOutputParser gamsParser, IOrdemClienteRepository ordemClienteRepo)
    {
        _gamsParser = gamsParser;
        _ordemClienteRepo = ordemClienteRepo;
    }

    public async Task<ResultadoGamsAnalisado> Handle(AnalisarResultadoGamsCommand request, CancellationToken cancellationToken)
    {
        // 1. Usa o parser para ler os dados brutos dos arquivos de resultado
        var dadosGams = await _gamsParser.ParseAsync(request.CaminhoPastaJob);

        // 2. Busca todas as ordens de cliente originais para fazer a junção
        var ordensOriginais = (await _ordemClienteRepo.GetAllAsync()).ToList();

        var planoDetalhado = new List<ItemDePlanoDetalhado>();

        // 3. Processa cada linha de status, conectando-a à ordem original
        foreach (var status in dadosGams.StatusDasEntregas)
        {
            // Encontra a(s) ordem(ns) de cliente original que corresponde(m) a esta demanda
            var ordensCorrespondentes = ordensOriginais.Where(o =>
                o.ItemRequisitado.PN_Generico == status.PN_Base &&
                o.ItemRequisitado.Largura == status.LarguraProduto &&
                o.ItemRequisitado.Comprimento == status.CompProduto &&
                o.DataEntrega.Date == status.DataEntregaRequerida.Date
            ).ToList();

            // Para cada ordem original, cria um item de plano detalhado
            foreach (var ordem in ordensCorrespondentes)
            {
                planoDetalhado.Add(new ItemDePlanoDetalhado(
                    NumeroOrdemCliente: ordem.NumeroOrdem, // A RASTREABILIDADE ACONTECE AQUI!
                    PN_Base: status.PN_Base,
                    LarguraProduto: status.LarguraProduto,
                    CompProduto: status.CompProduto,
                    DataEntregaRequerida: status.DataEntregaRequerida,
                    QtdDemandada: ordem.Quantidade, // Pega a quantidade original da ordem
                    DataProducaoReal: status.DataProducaoReal,
                    DiasDesvio: status.DiasDesvio,
                    StatusEntrega: TraduzirStatus(status.StatusEntrega)
                ));
            }
        }

        // A lógica de KPIs e da tabela de produção será adicionada aqui em breve
        return new ResultadoGamsAnalisado { PlanoDetalhado = planoDetalhado.OrderBy(p => p.DataEntregaRequerida).ToList() };
    }

    private string TraduzirStatus(int statusGams)
    {
        return statusGams switch
        {
            0 => "On Time",
            -1 => "Late / Not Planned",
            _ => "Unknown"
        };
    }
}