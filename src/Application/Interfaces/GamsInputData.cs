using APSSystem.Core.Entities;
using APSSystem.Core.ValueObjects;
using System;
using System.Collections.Generic;

namespace APSSystem.Application.Interfaces;

public class GamsInputData
{
    public IEnumerable<Recurso> Recursos { get; set; } = new List<Recurso>();
    // RENOMEADO para clareza
    public Dictionary<(string RecursoId, DateOnly Data), decimal> TempoDisponivelDiarioEmMinutos { get; set; } = new();
    public Dictionary<(PartNumber PartNumber, DateTime DataEntrega), int> DemandasAgregadas { get; set; } = new();
}