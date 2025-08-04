namespace APSSystem.Core.Enums;

/// <summary>
/// Define o tipo de uma exceção de calendário, indicando se ela
/// representa um tempo de parada ou um tempo de trabalho extra.
/// </summary>
public enum TipoExcecao
{
    /// <summary>
    /// O período de tempo especificado na exceção está indisponível para produção (ex: feriado, manutenção).
    /// </summary>
    Indisponivel,

    /// <summary>
    /// O período de tempo especificado na exceção está disponível para produção (ex: turno extra no fim de semana).
    /// </summary>
    Disponivel
}