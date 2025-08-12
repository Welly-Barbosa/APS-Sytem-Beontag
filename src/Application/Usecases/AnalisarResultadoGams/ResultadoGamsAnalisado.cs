using System;
using System.Collections.Generic;

namespace APSSystem.Application.UseCases.AnalisarResultadoGams;

/// <summary>
/// DTO que contém os resultados da otimização já processados e enriquecidos,
/// prontos para serem exibidos na tela de resultados.
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
public record ItemDePlanoDetalhado
{
    // Informações da Ordem Original
    public string NumeroOrdemCliente { get; init; } = string.Empty;
    public string PN_Base { get; init; } = string.Empty;
    public decimal LarguraProduto { get; init; }
    public decimal CompProduto { get; init; }
    public DateTime DataEntregaRequerida { get; init; }
    public decimal QtdDemandada { get; init; }

    // Informações do Plano Otimizado
    public DateTime? DataProducaoReal { get; init; }
    public int DiasDesvio { get; init; }
    public string StatusEntrega { get; init; } = string.Empty; // "On Time", "Late", "Not Planned"
}

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