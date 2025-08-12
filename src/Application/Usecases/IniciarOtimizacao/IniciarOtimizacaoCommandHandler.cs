using APSSystem.Application.Interfaces;
using APSSystem.Core.DTOs;
using APSSystem.Core.Entities;
using APSSystem.Core.Interfaces;
using APSSystem.Core.Services;
using APSSystem.Core.ValueObjects;
using MediatR;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace APSSystem.Application.UseCases.IniciarOtimizacao;

/// <summary>
/// Orquestra o ciclo completo de pré-processamento e execução da otimização GAMS.
/// </summary>
public class IniciarOtimizacaoCommandHandler : IRequestHandler<IniciarOtimizacaoCommand, GamsExecutionResult>
{
    private readonly IGamsExecutionService _gamsService;
    private readonly IScenarioService _scenarioService;
    private readonly IRecursoRepository _recursoRepo;
    private readonly ICalendarioRepository _calendarioRepo;
    private readonly IOrdemClienteRepository _ordemRepo;
    private readonly IItemDeInventarioRepository _inventarioRepo;
    private readonly AlocacaoInventarioService _alocacaoService;
    private readonly IDataValidationService _validationService;
    private readonly IConfiguration _configuration;

    public IniciarOtimizacaoCommandHandler(
        IGamsExecutionService gamsService,
        IScenarioService scenarioService,
        IRecursoRepository recursoRepo,
        ICalendarioRepository calendarioRepo,
        IOrdemClienteRepository ordemRepo,
        IItemDeInventarioRepository inventarioRepo,
        AlocacaoInventarioService alocacaoService,
        IDataValidationService validationService,
        IConfiguration configuration)
    {
        _gamsService = gamsService;
        _scenarioService = scenarioService;
        _recursoRepo = recursoRepo;
        _calendarioRepo = calendarioRepo;
        _ordemRepo = ordemRepo;
        _inventarioRepo = inventarioRepo;
        _alocacaoService = alocacaoService;
        _validationService = validationService;
        _configuration = configuration;
    }

    public async Task<GamsExecutionResult> Handle(IniciarOtimizacaoCommand request, CancellationToken cancellationToken)
    {
        // Define o cenário para que os repositórios leiam os arquivos corretos
        _scenarioService.DefinirCenario(request.Cenario);

        // Executa a pipeline de pré-processamento
        var dadosParaGams = await PrepararDadosDeEntrada(request.DataInicio, request.DataFim);

        // Lê as configurações para a execução
        var gamsModelPath = _configuration.GetValue<string>("GamsSettings:ModelSourcePath");
        var timeoutMinutes = _configuration.GetValue<int>("GamsSettings:TimeoutMinutes");

        if (string.IsNullOrEmpty(gamsModelPath))
            throw new InvalidOperationException("Caminho do modelo GAMS (.gms) não configurado no appsettings.json.");

        // ETAPA FINAL: CARGA (Execução do GAMS)
        var resultadoExecucao = await _gamsService.ExecutarAsync(
            gamsModelPath,
            dadosParaGams,
            TimeSpan.FromMinutes(timeoutMinutes));

        if (!resultadoExecucao.Sucesso)
        {
            // Se a execução do GAMS falhou, lança uma exceção para a UI capturar e exibir
            throw new InvalidOperationException($"A execução do GAMS falhou: {resultadoExecucao.MensagemErro}");
        }

        return resultadoExecucao;
    }

    /// <summary>
    /// Orquestra as etapas de Extração, Validação e Transformação dos dados.
    /// </summary>
    private async Task<GamsInputData> PrepararDadosDeEntrada(DateOnly dataInicio, DateOnly dataFim)
    {
        // --- ETAPA 1: EXTRAÇÃO ---
        var recursos = (await _recursoRepo.GetAllAsync()).ToList();
        var calendarios = (await _calendarioRepo.GetAllAsync()).ToList();
        var todasAsOrdens = (await _ordemRepo.GetAllAsync()).ToList();

        // --- ETAPA 2: VALIDAÇÃO ---
        var validationResult = _validationService.ValidateResourcesAndCalendars(recursos, calendarios);
        if (!validationResult.IsValid)
        {
            // Falha rápido com uma mensagem de erro clara se os dados estiverem inconsistentes
            throw new InvalidOperationException($"Falha na validação dos dados de entrada: {validationResult.ErrorMessage}");
        }

        // --- ETAPA 3: TRANSFORMAÇÃO ---

        // 3a. Identifica as datas relevantes (lógica "esparsa")
        var datasRelevantes = new HashSet<DateOnly>();
        var datasDasOrdens = todasAsOrdens.Select(o => DateOnly.FromDateTime(o.DataEntrega)).Where(d => d >= dataInicio && d <= dataFim);
        foreach (var data in datasDasOrdens) { datasRelevantes.Add(data); }

        for (var data = dataInicio; data <= dataFim; data = data.AddDays(1))
        {
            if (recursos.Any(r => (calendarios.FirstOrDefault(c => c.Id == r.CalendarioId)?.CalcularHorasDisponiveis(data) ?? 0) > 0))
            {
                datasRelevantes.Add(data);
            }
        }
        var listaDeDatasOrdenada = datasRelevantes.OrderBy(d => d).ToList();

        // 3b. Calcula a capacidade apenas para as datas relevantes
        var minutosDisponiveisCalculado = new Dictionary<(string RecursoId, DateOnly Data), decimal>();
        foreach (var data in listaDeDatasOrdenada)
        {
            foreach (var recurso in recursos)
            {
                var calendario = calendarios.First(c => c.Id == recurso.CalendarioId); // Busca segura, pois já validamos
                minutosDisponiveisCalculado.Add((recurso.Id, data), calendario.CalcularHorasDisponiveis(data) * 60);
            }
        }

        // 3c. Calcula a demanda líquida agregada
        var ordensFiltradas = todasAsOrdens
            .Where(o => (o.ItemRequisitado.Comprimento == 10000 || o.ItemRequisitado.Comprimento == 15000) &&
                        datasRelevantes.Contains(DateOnly.FromDateTime(o.DataEntrega)))
            .ToList();

        var necessidadesIndividuais = new List<NecessidadeDeProducao>();
        foreach (var ordem in ordensFiltradas)
        {
            var inventarioDisponivel = await _inventarioRepo.GetByPNGenericoAsync(ordem.ItemRequisitado.PN_Generico);
            var resultadoAlocacao = _alocacaoService.Alocar(ordem, inventarioDisponivel);

            if (resultadoAlocacao.NecessidadeLiquidaFinal > 0)
            {
                var partNumberEfetivo = new PartNumber(ordem.ItemRequisitado.PN_Generico, ordem.LarguraCorte, ordem.ItemRequisitado.Comprimento);
                necessidadesIndividuais.Add(new NecessidadeDeProducao(ordem.NumeroOrdem, partNumberEfetivo, ordem.DataEntrega, resultadoAlocacao.NecessidadeLiquidaFinal));
            }
        }

        var demandasAgregadas = necessidadesIndividuais
            .GroupBy(n => new { n.PartNumber, n.DataEntrega })
            .ToDictionary(g => (g.Key.PartNumber, g.Key.DataEntrega), g => g.Sum(n => n.QuantidadeLiquida));

        // --- ETAPA 4: EMPACOTAR DADOS TRANSFORMADOS ---
        return new GamsInputData
        {
            Recursos = recursos,
            TempoDisponivelDiarioEmMinutos = minutosDisponiveisCalculado,
            DemandasAgregadas = demandasAgregadas
        };
    }
}