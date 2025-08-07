using APSSystem.Application.Interfaces;
using APSSystem.Core.Entities;
using APSSystem.Core.Interfaces;
using APSSystem.Core.Services;
using APSSystem.Core.ValueObjects;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace APSSystem.Application.UseCases.ExecutarOtimizacao;

public class ExecutarOtimizacaoCommandHandler : IRequestHandler<ExecutarOtimizacaoCommand>
{
    private readonly IGamsExecutionService _gamsService;
    private readonly IScenarioService _scenarioService;
    private readonly IRecursoRepository _recursoRepo;
    private readonly ICalendarioRepository _calendarioRepo;
    private readonly IOrdemClienteRepository _ordemRepo;
    private readonly IItemDeInventarioRepository _inventarioRepo;
    private readonly AlocacaoInventarioService _alocacaoService;

    // Caminhos de configuração (em um sistema real, viriam de um arquivo de configuração)
    private const string GamsModelSourcePath = @"C:\Path\To\Your\Model\modelo_aps.gms";
    private const int GamsTimeoutMinutes = 5;


    public ExecutarOtimizacaoCommandHandler(
        IGamsExecutionService gamsService,
        IScenarioService scenarioService,
        IRecursoRepository recursoRepo,
        ICalendarioRepository calendarioRepo,
        IOrdemClienteRepository ordemRepo,
        IItemDeInventarioRepository inventarioRepo,
        AlocacaoInventarioService alocacaoService)
    {
        _gamsService = gamsService;
        _scenarioService = scenarioService;
        _recursoRepo = recursoRepo;
        _calendarioRepo = calendarioRepo;
        _ordemRepo = ordemRepo;
        _inventarioRepo = inventarioRepo;
        _alocacaoService = alocacaoService;
    }

    public async Task Handle(ExecutarOtimizacaoCommand request, CancellationToken cancellationToken)
    {
        // Passo 1: Definir o cenário para que os repositórios leiam os arquivos corretos
        _scenarioService.DefinirCenario(request.Cenario);

        // Passo 2: Coletar e processar todos os dados para o GAMS (lógica de pré-processamento)
        var dadosParaGams = await PrepararDadosDeEntrada(request.DataInicio, request.DataFim);

        // Passo 3: Chamar o serviço de execução do GAMS
        var resultadoExecucao = await _gamsService.ExecutarAsync(
            GamsModelSourcePath,
            dadosParaGams,
            TimeSpan.FromMinutes(GamsTimeoutMinutes));

        if (!resultadoExecucao.Sucesso)
        {
            // Se a execução do GAMS falhou, lança uma exceção para a UI capturar
            throw new InvalidOperationException($"A execução do GAMS falhou: {resultadoExecucao.MensagemErro}");
        }

        // Passo 4 (Futuro): Disparar o comando de pós-processamento
        // await _mediator.Send(new ProcessarResultadoGamsCommand(resultadoExecucao.CaminhoPastaJob), cancellationToken);

        Console.WriteLine($"Execução do GAMS concluída com sucesso. Resultados estão em: {resultadoExecucao.CaminhoPastaJob}");
    }

    /// <summary>
    /// Reutiliza a mesma lógica do nosso pré-processador de dashboard para coletar e agregar dados.
    /// </summary>
    private async Task<GamsInputData> PrepararDadosDeEntrada(DateOnly dataInicio, DateOnly dataFim)
    {
        // 1. Calcular Demanda Líquida Agregada
        var todasAsOrdens = await _ordemRepo.GetAllAsync();
        var ordensFiltradas = todasAsOrdens
            .Where(o =>
                (o.ItemRequisitado.Comprimento == 10000 || o.ItemRequisitado.Comprimento == 15000) &&
                DateOnly.FromDateTime(o.DataEntrega) >= dataInicio &&
                DateOnly.FromDateTime(o.DataEntrega) <= dataFim)
            .ToList();

        var necessidadesIndividuais = new List<NecessidadeDeProducao>();
        foreach (var ordem in ordensFiltradas)
        {
            var inventarioDisponivel = await _inventarioRepo.GetByPNGenericoAsync(ordem.ItemRequisitado.PN_Generico);
            var resultadoAlocacao = _alocacaoService.Alocar(ordem, inventarioDisponivel);

            if (resultadoAlocacao.NecessidadeLiquidaFinal > 0)
            {
                var partNumberEfetivo = new PartNumber(
                    ordem.ItemRequisitado.PN_Generico,
                    ordem.LarguraCorte,
                    ordem.ItemRequisitado.Comprimento
                );
                necessidadesIndividuais.Add(new NecessidadeDeProducao(
                    ordem.NumeroOrdem, partNumberEfetivo, ordem.DataEntrega, resultadoAlocacao.NecessidadeLiquidaFinal));
            }
        }

        var demandasAgregadas = necessidadesIndividuais
            .GroupBy(n => new { n.PartNumber, n.DataEntrega })
            .ToDictionary(g => (g.Key.PartNumber, g.Key.DataEntrega), g => g.Sum(n => n.QuantidadeLiquida));

        // 2. Calcular Capacidade dos Recursos
        var recursos = await _recursoRepo.GetAllAsync();
        var minutosDisponiveisCalculado = new Dictionary<(string RecursoId, DateOnly Data), decimal>();
        foreach (var recurso in recursos)
        {
            var calendario = await _calendarioRepo.GetByIdAsync(recurso.CalendarioId);
            if (calendario != null)
            {
                for (var data = dataInicio; data <= dataFim; data = data.AddDays(1))
                {
                    minutosDisponiveisCalculado.Add((recurso.Id, data), calendario.CalcularHorasDisponiveis(data) * 60);
                }
            }
        }

        // 3. Empacotar no DTO
        return new GamsInputData
        {
            Recursos = recursos,
            TempoDisponivelDiarioEmMinutos = minutosDisponiveisCalculado,
            DemandasAgregadas = demandasAgregadas
        };
    }
}