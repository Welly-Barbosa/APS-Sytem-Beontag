using System;

namespace APSSystem.Application.UseCases.ObterDetalhesRecurso;

/// <summary>
/// DTO com as informações detalhadas de um recurso e sua disponibilidade calculada.
/// </summary>
public class DetalhesRecursoResult
{
    public bool RecursoEncontrado { get; set; }
    public string? MensagemErro { get; set; }
    public string? RecursoId { get; set; }
    public string? DescricaoRecurso { get; set; }
    public DateOnly? DataConsultada { get; set; }
    public decimal HorasDisponiveisNoDia { get; set; }
}