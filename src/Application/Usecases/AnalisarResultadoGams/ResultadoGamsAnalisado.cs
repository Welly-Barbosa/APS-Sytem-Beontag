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
    public decimal TotalWasteInches { get; set; } // Supondo que a unidade seja polegadas

    // Dados para as Tabelas e Gráfico
    public List<ItemDePlanoDetalhado> PlanoCliente { get; set; } = new();
    public List<ItemOrdemProducao> PlanoProducao { get; set; } = new();
}

/// <summary>
/// Representa a visão de uma Ordem de Cliente e seu status final.
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
    string ComposicaoDoCorte, // Ex: "3x PROD-A-10, 2x PROD-B-15"
    decimal PerdaMaterialPercentual
);