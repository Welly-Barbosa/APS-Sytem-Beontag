using MediatR;
using System;

namespace APSSystem.Application.UseCases.ObterDetalhesRecurso;

/// <summary>
/// Query para obter os detalhes de um recurso e sua disponibilidade em uma data específica.
/// </summary>
/// <param name="RecursoId">O ID do recurso a ser consultado.</param>
/// <param name="DataParaConsulta">A data para a qual a disponibilidade deve ser calculada.</param>
public record ObterDetalhesRecursoQuery(string RecursoId, DateOnly DataParaConsulta)
    : IRequest<DetalhesRecursoResult>;