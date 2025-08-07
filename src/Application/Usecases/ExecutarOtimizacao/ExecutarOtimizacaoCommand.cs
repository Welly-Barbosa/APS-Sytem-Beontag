using APSSystem.Core.Enums;
using MediatR;
using System;

namespace APSSystem.Application.UseCases.ExecutarOtimizacao;

/// <summary>
/// Representa o comando para iniciar um ciclo de otimização completo do GAMS.
/// </summary>
/// <param name="Cenario">O cenário de dados a ser utilizado.</param>
/// <param name="DataInicio">O início do horizonte de planejamento.</param>
/// <param name="DataFim">O fim do horizonte de planejamento.</param>
public record ExecutarOtimizacaoCommand(
    CenarioTipo Cenario,
    DateOnly DataInicio,
    DateOnly DataFim) : IRequest;