namespace APSSystem.Core.ValueObjects;

/// <summary>
/// Representa um período de trabalho recorrente em um dia específico da semana.
/// Este é um Objeto de Valor, pois é definido por seus atributos.
/// </summary>
/// <param name="DiaDaSemana">O dia da semana em que este turno ocorre.</param>
/// <param name="HoraInicio">A hora de início do turno.</param>
/// <param name="HoraFim">A hora de término do turno.</param>
public record Turno(
    DayOfWeek DiaDaSemana,
    TimeOnly HoraInicio,
    TimeOnly HoraFim)
{
    /// <summary>
    /// Calcula a duração total do turno em horas.
    /// </summary>
    public decimal DuracaoEmHoras => (decimal)(HoraFim - HoraInicio).TotalHours;
}

