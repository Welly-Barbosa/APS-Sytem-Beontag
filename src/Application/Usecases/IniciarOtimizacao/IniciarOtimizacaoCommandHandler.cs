using APSSystem.Application.Interfaces;
using APSSystem.Application.UseCases.GerarArquivoGams;
using MediatR;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace APSSystem.Application.UseCases.IniciarOtimizacao;

/// <summary>
/// Orquestra o processo de otimização: define o cenário, gera o arquivo de entrada e simula a execução.
/// </summary>
public class IniciarOtimizacaoCommandHandler : IRequestHandler<IniciarOtimizacaoCommand>
{
    private readonly IMediator _mediator;
    private readonly IScenarioService _scenarioService;

    public IniciarOtimizacaoCommandHandler(IMediator mediator, IScenarioService scenarioService)
    {
        _mediator = mediator;
        _scenarioService = scenarioService;
    }

    public async Task Handle(IniciarOtimizacaoCommand request, CancellationToken cancellationToken)
    {
        // 1. Define o cenário ativo para toda a operação
        _scenarioService.DefinirCenario(request.Cenario);

        // 2. Define o caminho de saída e chama o caso de uso de baixo nível para gerar o arquivo
        string caminhoDeSaida = Path.Combine(AppContext.BaseDirectory, "GamsInputData.dat");
        var gerarArquivoCommand = new GerarArquivoGamsCommand(caminhoDeSaida, request.DataInicio, request.DataFim);
        await _mediator.Send(gerarArquivoCommand, cancellationToken);

        // 3. Futuramente, a chamada ao IGamsExecutionService entraria aqui.
        // await _gamsService.ExecutarAsync(...);
    }
}