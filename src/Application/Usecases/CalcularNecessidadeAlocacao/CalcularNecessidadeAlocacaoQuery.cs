using MediatR;

namespace APSSystem.Application.UseCases.CalcularNecessidadeAlocacao;

/// <summary>
/// Query que representa a solicitação para calcular a necessidade líquida,
/// considerando a alocação de inventário.
/// </summary>
/// <param name="NumeroOrdem">O número da ordem de cliente.</param>
public record CalcularNecessidadeAlocacaoQuery(string NumeroOrdem) : IRequest<ResultadoAlocacaoDto>;