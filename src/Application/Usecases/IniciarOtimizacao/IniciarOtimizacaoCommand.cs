using APSSystem.Application.Interfaces; // Adicionar este using
using APSSystem.Core.Enums;
using MediatR;
using System;

namespace APSSystem.Application.UseCases.IniciarOtimizacao;

// ALTERADO: O comando agora retorna um GamsExecutionResult
public record IniciarOtimizacaoCommand(
    CenarioTipo Cenario,
    DateOnly DataInicio,
    DateOnly DataFim) : IRequest<GamsExecutionResult>;