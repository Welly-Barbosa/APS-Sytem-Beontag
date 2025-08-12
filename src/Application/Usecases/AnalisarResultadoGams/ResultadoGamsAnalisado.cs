using System;
using System.Collections.Generic;

namespace APSSystem.Application.UseCases.AnalisarResultadoGams;

/// <summary>
/// DTO que contém os resultados da otimização já processados e enriquecidos.
/// </summary>
public class ResultadoGamsAnalisado
{
    // KPIs de Alto Nível
    public decimal OrderFulfillmentPercentage { get; set; }
    public decimal AverageWastePercentage { get; set; }
    public decimal TotalWasteInches { get; set; }

    // DADOS PARA AS TABELAS - AQUI ESTÁ A PROPRIEDADE QUE FALTAVA
    public List<ItemDePlanoDetalhado> PlanoCliente { get; init; } = new();
    public List<ItemOrdemProducao> PlanoProducao { get; init; } = new();
}

/// <summary>
/// Representa a visão de uma Ordem de Cliente e seu status final.
/// CORRIGIDO: Agora usa um construtor primário, que corresponde à forma como é chamado.
/// </summary>
public record ItemDePlanoDetalhado(
    string NumeroOrdemCliente,
    string PN_Base,
    decimal LarguraProduto,
    decimal CompProduto,
    DateTime DataEntregaRequerida,
    decimal QtdDemandada,
    DateTime? DataProducaoReal,
    int DiasDesvio,
    string StatusEntrega
);

/// <summary>
/// Representa uma Ordem de Produção com seu padrão de corte e perda calculada.
/// </summary>
public record ItemOrdemProducao(
    DateTime DataProducao,
    string Maquina,
    string PadraoCorte,
    decimal QtdBobinasMae,
    string ComposicaoDoCorte,
    decimal PerdaMaterialPercentual
);