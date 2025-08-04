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

namespace APSSystem.Application.UseCases.GerarArquivoGams;

public class GerarArquivoGamsCommandHandler : IRequestHandler<GerarArquivoGamsCommand>
{
    // --- DECLARAÇÃO DOS CAMPOS QUE ESTAVAM FALTANDO ---
    private readonly IRecursoRepository _recursoRepo;
    private readonly ICalendarioRepository _calendarioRepo;
    private readonly IOrdemClienteRepository _ordemRepo;
    private readonly IItemDeInventarioRepository _inventarioRepo;
    private readonly AlocacaoInventarioService _alocacaoService;
    private readonly INecessidadeDeProducaoRepository _necessidadeRepo;
    private readonly IGamsFileWriter _gamsWriter;

    // Construtor completo com todas as injeções de dependência
    public GerarArquivoGamsCommandHandler(
        IRecursoRepository recursoRepo, ICalendarioRepository calendarioRepo,
        IOrdemClienteRepository ordemRepo, IItemDeInventarioRepository inventarioRepo,
        AlocacaoInventarioService alocacaoService, INecessidadeDeProducaoRepository necessidadeRepo,
        IGamsFileWriter gamsWriter)
    {
        _recursoRepo = recursoRepo;
        _calendarioRepo = calendarioRepo;
        _ordemRepo = ordemRepo;
        _inventarioRepo = inventarioRepo;
        _alocacaoService = alocacaoService;
        _necessidadeRepo = necessidadeRepo;
        _gamsWriter = gamsWriter;
    }

    // Lógica completa e final do método Handle
    public async Task Handle(GerarArquivoGamsCommand request, CancellationToken cancellationToken)
    {
        // --- FASE 1: MAPEAMENTO E CÁLCULO DA NECESSIDADE LÍQUIDA ---
        var todasAsOrdens = await _ordemRepo.GetAllAsync();
        var ordensFiltradas = todasAsOrdens
            .Where(o =>
            {
                bool comprimentoValido = o.ItemRequisitado.Comprimento == 10000 || o.ItemRequisitado.Comprimento == 15000;
                if (!comprimentoValido) return false;

                var dataEntrega = DateOnly.FromDateTime(o.DataEntrega);
                bool dataValida = dataEntrega >= request.DataInicio && dataEntrega <= request.DataFim;
                return dataValida;
            })
            .ToList();

        var necessidadesIndividuais = new List<NecessidadeDeProducao>();
        foreach (var ordem in ordensFiltradas)
        {
            int necessidadeLiquida = ordem.Quantidade;
            if (necessidadeLiquida > 0)
            {
                var partNumberEfetivo = new PartNumber(
                    ordem.ItemRequisitado.PN_Generico,
                    ordem.LarguraCorte,
                    ordem.ItemRequisitado.Comprimento
                );

                // Em um sistema real, salvaríamos isso no banco de dados aqui.
                // Para o PoC, apenas criamos a lista para a próxima etapa.
                necessidadesIndividuais.Add(new NecessidadeDeProducao(
                    ordem.NumeroOrdem,
                    partNumberEfetivo,
                    ordem.DataEntrega,
                    necessidadeLiquida));
            }
        }

        // --- FASE 2: AGREGAÇÃO DA DEMANDA ---
        var demandasAgregadas = necessidadesIndividuais
            .GroupBy(n => new { n.PartNumber, n.DataEntrega })
            .ToDictionary(
                g => (g.Key.PartNumber, g.Key.DataEntrega),
                g => g.Sum(n => n.QuantidadeLiquida)
            );

        // --- Lógica de cálculo de capacidade ---
        var recursos = await _recursoRepo.GetAllAsync();
        var minutosDisponiveisCalculado = new Dictionary<(string RecursoId, DateOnly Data), decimal>();
        foreach (var recurso in recursos)
        {
            var calendario = await _calendarioRepo.GetByIdAsync(recurso.CalendarioId);
            if (calendario != null)
            {
                for (var data = request.DataInicio; data <= request.DataFim; data = data.AddDays(1))
                {
                    decimal horasDisponiveis = calendario.CalcularHorasDisponiveis(data);
                    decimal minutosDisponiveis = horasDisponiveis * 60;
                    minutosDisponiveisCalculado.Add((recurso.Id, data), minutosDisponiveis);
                }
            }
        }

        // --- FASE 3: EMPACOTAR E GERAR ARQUIVO ---
        var dadosParaGams = new GamsInputData
        {
            Recursos = recursos,
            TempoDisponivelDiarioEmMinutos = minutosDisponiveisCalculado,
            DemandasAgregadas = demandasAgregadas
        };

        await _gamsWriter.GerarArquivoDeEntradaAsync(request.CaminhoArquivoSaida, dadosParaGams);
    }
}