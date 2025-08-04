namespace APSSystem.Core.Entities;

/// <summary>
/// Representa um recurso produtivo (ex: máquina, linha de montagem, célula de trabalho).
/// </summary>
public record Recurso(
    string Id,
    string Descricao,
    decimal VelocidadePolPorMinuto, // UNIDADE CORRIGIDA
    decimal Eficiencia,
    decimal TempoDeSetupEmMinutos, // UNIDADE CORRIGIDA
    int MaximoCortes,
    decimal CustoPorHora,
    string CalendarioId
);