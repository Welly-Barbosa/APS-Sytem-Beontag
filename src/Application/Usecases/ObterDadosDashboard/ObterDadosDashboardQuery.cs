using MediatR;
using System;

namespace APSSystem.Application.UseCases.ObterDadosDashboard;

/// <summary>
/// Representa a solicitação para buscar todos os dados agregados para o dashboard.
/// </summary>
/// <param name="DataInicio">O início do horizonte de planejamento.</param>
/// <param name="DataFim">O fim do horizonte de planejamento.</param>
public record ObterDadosDashboardQuery(DateOnly DataInicio, DateOnly DataFim) : IRequest<DadosDashboardResult>;