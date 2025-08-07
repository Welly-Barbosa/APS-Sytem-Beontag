using APSSystem.Core.Enums;
using MediatR;
using System;

namespace APSSystem.Application.UseCases.IniciarOtimizacao;

/// <summary>
/// Comando de alto nível para iniciar todo o ciclo de otimização.
/// </summary>
public record IniciarOtimizacaoCommand(
    CenarioTipo Cenario,
    DateOnly DataInicio,
    DateOnly DataFim) : IRequest;