using APSSystem.Core.Entities;
using System;
using System.Collections.Generic;

namespace APSSystem.Application.UseCases.ObterDadosDashboard;

/// <summary>
/// DTO que contém todos os dados processados para a exibição no dashboard.
/// </summary>
public class DadosDashboardResult
{
    public List<PontoDeDadosDiario> PontosDeDados { get; set; } = new();
    public List<OrdemCliente> OrdensNoHorizonte { get; set; } = new();

    // --- PROPRIEDADES QUE FALTAVAM ---
    public decimal CapacidadeTotalGeral { get; set; }
    public decimal DemandaTotalGeral { get; set; }
}

/// <summary>
/// Representa os totais de capacidade e demanda para um único dia.
/// </summary>
public record PontoDeDadosDiario(
    DateOnly Data,
    Dictionary<string, decimal> CapacidadePorRecurso,
    decimal DemandaTotal
);