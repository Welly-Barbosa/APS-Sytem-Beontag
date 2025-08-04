using MediatR;
using System; // Adicionar

namespace APSSystem.Application.UseCases.GerarArquivoGams;

/// <param name="CaminhoArquivoSaida">O caminho completo onde o arquivo deve ser salvo.</param>
/// <param name="DataInicio">O início do horizonte de planejamento.</param>
/// <param name="DataFim">O fim do horizonte de planejamento.</param>
public record GerarArquivoGamsCommand(
    string CaminhoArquivoSaida,
    DateOnly DataInicio,
    DateOnly DataFim) : IRequest;