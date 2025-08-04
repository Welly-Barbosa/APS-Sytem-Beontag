using System;
using APSSystem.Core.Enums; // <-- ESTA É A LINHA DA CORREÇÃO

namespace APSSystem.Core.Entities;

/// <summary>
/// Representa uma exceção às regras de turno padrão de um calendário,
/// como um feriado, uma parada para manutenção ou um turno extra.
/// </summary>
/// <param name="Data">A data específica em que a exceção ocorre.</param>
/// <param name="Tipo">O tipo da exceção (Disponível ou Indisponível).</param>
/// <param name="Descricao">Uma descrição do motivo da exceção (ex: "Feriado de Natal").</param>
/// <param name="HoraInicio">A hora de início da exceção. Se nulo, aplica-se desde o início do dia.</param>
/// <param name="HoraFim">A hora de fim da exceção. Se nulo, aplica-se até o fim do dia.</param>
public record ExcecaoCalendario(
    DateOnly Data,
    TipoExcecao Tipo, // <-- Agora o compilador sabe o que é TipoExcecao
    string Descricao,
    TimeOnly? HoraInicio = null,
    TimeOnly? HoraFim = null);