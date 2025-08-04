using APSSystem.Core.Enums;
using APSSystem.Core.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace APSSystem.Core.Entities;

/// <summary>
/// Representa um calendário de disponibilidade, contendo as regras de turnos de trabalho e exceções.
/// </summary>
public class Calendario
{
    public string Id { get; private set; }
    public string Descricao { get; private set; }

    private readonly List<Turno> _turnos = new();
    public IReadOnlyCollection<Turno> Turnos => _turnos.AsReadOnly();

    private readonly List<ExcecaoCalendario> _excecoes = new();
    public IReadOnlyCollection<ExcecaoCalendario> Excecoes => _excecoes.AsReadOnly();

    public Calendario(string id, string descricao)
    {
        Id = !string.IsNullOrWhiteSpace(id) ? id : throw new ArgumentException("ID do calendário não pode ser nulo ou vazio.", nameof(id));
        Descricao = descricao;
    }

    public void AdicionarTurno(Turno turno)
    {
        _turnos.Add(turno);
    }

    /// <summary>
    /// Adiciona uma nova exceção (feriado, manutenção, etc.) a este calendário.
    /// </summary>
    /// <param name="excecao">A exceção a ser adicionada.</param>
    public void AdicionarExcecao(ExcecaoCalendario excecao)
    {
        _excecoes.Add(excecao);
    }

    /// <summary>
    /// Calcula o total de horas de trabalho disponíveis em uma data específica,
    /// considerando os turnos padrão e quaisquer exceções.
    /// </summary>
    /// <param name="data">A data para a qual a disponibilidade será calculada.</param>
    /// <returns>O total de horas disponíveis como um valor decimal.</returns>
    public decimal CalcularHorasDisponiveis(DateOnly data)
    {
        // 1. Verifica se existe uma exceção de dia inteiro indisponível (ex: feriado).
        var feriado = _excecoes.FirstOrDefault(e => e.Data == data && e.Tipo == TipoExcecao.Indisponivel && !e.HoraInicio.HasValue);
        if (feriado != null)
        {
            return 0; // Se for feriado, a disponibilidade é zero.
        }

        // 2. Encontra os turnos padrão para aquele dia da semana.
        var turnosDoDia = _turnos.Where(t => t.DiaDaSemana == data.DayOfWeek);
        decimal horasDisponiveis = turnosDoDia.Sum(t => t.DuracaoEmHoras);

        // 3. Subtrai as horas de exceções de indisponibilidade (ex: manutenções parciais).
        var paradasNoDia = _excecoes.Where(e => e.Data == data && e.Tipo == TipoExcecao.Indisponivel && e.HoraInicio.HasValue);
        foreach (var parada in paradasNoDia)
        {
            horasDisponiveis -= (decimal)(parada.HoraFim!.Value - parada.HoraInicio!.Value).TotalHours;
        }

        // 4. Adiciona as horas de exceções de disponibilidade (ex: turnos extras).
        var turnosExtrasNoDia = _excecoes.Where(e => e.Data == data && e.Tipo == TipoExcecao.Disponivel);
        foreach (var extra in turnosExtrasNoDia)
        {
            horasDisponiveis += (decimal)(extra.HoraFim!.Value - extra.HoraInicio!.Value).TotalHours;
        }

        // Garante que não retornaremos um valor negativo.
        return Math.Max(0, horasDisponiveis);
    }
}