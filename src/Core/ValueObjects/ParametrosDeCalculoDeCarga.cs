namespace APSSystem.Core.ValueObjects;

/// <summary>
/// Contém os parâmetros globais usados no cálculo de ocupação de máquina.
/// </summary>
public record ParametrosDeCalculoDeCarga(
    decimal LarguraBobinaMae,
    decimal FatorDePerda,
    int TempoProcessamentoBobina10k, // em minutos
    int TempoProcessamentoBobina15k, // em minutos
    int TempoSetupPorBobina, // em minutos
    decimal LarguraBobinaMaeGams // em polegadas
);