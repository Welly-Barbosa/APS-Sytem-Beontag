using MediatR;

namespace APSSystem.Application.UseCases.AnalisarResultadoGams;

/// <summary>
/// Representa o comando para iniciar a análise dos arquivos de resultado de uma execução do GAMS.
/// </summary>
/// <param name="CaminhoPastaJob">O caminho para a pasta que contém os arquivos .put de resultado.</param>
public record AnalisarResultadoGamsCommand(string CaminhoPastaJob) : IRequest<ResultadoGamsAnalisado>;