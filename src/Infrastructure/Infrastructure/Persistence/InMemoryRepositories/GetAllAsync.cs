using APSSystem.Core.Entities;
using APSSystem.Core.Interfaces;
using APSSystem.Core.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace APSSystem.Infrastructure.Persistence.InMemoryRepositories;

public class InMemoryCalendarioRepository : ICalendarioRepository
{
    private readonly List<Calendario> _calendarios = new();

    public InMemoryCalendarioRepository()
    {
        // Calendário 1: 5x8
        var cal5x8 = new Calendario("CAL-PADRAO-5x8", "Calendário Padrão 5x8 (Seg-Sex, 08:00-12:00 e 13:00-17:00)");
        var dias5x8 = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
        foreach (var dia in dias5x8)
        {
            cal5x8.AdicionarTurno(new Turno(dia, new TimeOnly(8, 0), new TimeOnly(12, 0)));
            cal5x8.AdicionarTurno(new Turno(dia, new TimeOnly(13, 0), new TimeOnly(17, 0)));
        }
        _calendarios.Add(cal5x8);

        // Calendário 2: 24x5
        var cal24x5 = new Calendario("CAL-24x5", "Calendário de Produção Contínua (3 Turnos, Seg-Sex)");
        var dias24x5 = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
        foreach (var dia in dias24x5)
        {
            cal24x5.AdicionarTurno(new Turno(dia, new TimeOnly(6, 0), new TimeOnly(14, 0)));
            cal24x5.AdicionarTurno(new Turno(dia, new TimeOnly(14, 0), new TimeOnly(22, 0)));
            cal24x5.AdicionarTurno(new Turno(dia, new TimeOnly(22, 0), new TimeOnly(23, 59, 59)));
        }
        _calendarios.Add(cal24x5);
    }

    public Task<Calendario?> GetByIdAsync(string id)
    {
        return Task.FromResult(_calendarios.FirstOrDefault(c => c.Id.Equals(id, StringComparison.OrdinalIgnoreCase)));
    }

    // --- NOVO MÉTODO ADICIONADO AQUI ---
    public Task<IEnumerable<Calendario>> GetAllAsync()
    {
        return Task.FromResult(_calendarios.AsEnumerable());
    }
}