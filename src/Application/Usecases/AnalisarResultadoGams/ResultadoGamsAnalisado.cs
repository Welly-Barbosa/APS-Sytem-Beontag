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
    string CustomerOrderNumber,
    string Product,
    decimal Length,
    decimal CuttingWidth,
    DateTime RequiredDate,
    int? RequiredQuantity,
    DateTime? PlannedDate,
    int? Deviation,
    string Status
);


/// <summary>
/// Representa uma Ordem de Produção com seu padrão de corte e perda calculada.
/// </summary>
public record ItemOrdemProducao(
    DateTime DataProducao,
    string Maquina,
    string JobNumber,
    string Product,
    decimal Length,
    int? QtdBobinasMae,
    string Composition,
    decimal WastePercentage
);