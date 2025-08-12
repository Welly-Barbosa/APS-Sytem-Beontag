using APSSystem.Core.Interfaces;
using APSSystem.Core.Services;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace APSSystem.Application.UseCases.ObterDadosDashboard;

public class ObterDadosDashboardQueryHandler : IRequestHandler<ObterDadosDashboardQuery, DadosDashboardResult>
{
    private readonly IRecursoRepository _recursoRepo;
    private readonly ICalendarioRepository _calendarioRepo;
    private readonly IOrdemClienteRepository _ordemRepo;
    private readonly ICalculadoraDeCargaService _calculadoraDeCarga;

    public ObterDadosDashboardQueryHandler(IRecursoRepository recursoRepo, ICalendarioRepository calendarioRepo, IOrdemClienteRepository ordemRepo, ICalculadoraDeCargaService calculadoraDeCarga)
    {
        _recursoRepo = recursoRepo;
        _calendarioRepo = calendarioRepo;
        _ordemRepo = ordemRepo;
        _calculadoraDeCarga = calculadoraDeCarga;
    }

    public async Task<DadosDashboardResult> Handle(ObterDadosDashboardQuery request, CancellationToken cancellationToken)
    {
        // --- Etapa 1: Coleta de Dados Brutos (Preservado do seu arquivo) ---
        var recursos = (await _recursoRepo.GetAllAsync()).ToList();
        var todasAsOrdens = (await _ordemRepo.GetAllAsync()).ToList();

        // --- ETAPA 2: LÓGICA DE FILTRAGEM DE DATAS (A Nova Lógica Aplicada) ---
        var datasRelevantes = new HashSet<DateOnly>();

        // Adiciona as datas de entrega das ordens que estão dentro da janela de planejamento
        var datasDasOrdens = todasAsOrdens
            .Select(o => DateOnly.FromDateTime(o.DataEntrega))
            .Where(d => d >= request.DataInicio && d <= request.DataFim);
        foreach (var data in datasDasOrdens)
        {
            datasRelevantes.Add(data);
        }

        // Adiciona os dias com capacidade de produção dentro da janela
        for (var data = request.DataInicio; data <= request.DataFim; data = data.AddDays(1))
        {
            decimal capacidadeTotalDoDia = 0;
            foreach (var recurso in recursos)
            {
                var calendario = await _calendarioRepo.GetByIdAsync(recurso.CalendarioId);
                if (calendario != null)
                {
                    capacidadeTotalDoDia += calendario.CalcularHorasDisponiveis(data);
                }
            }
            if (capacidadeTotalDoDia > 0)
            {
                datasRelevantes.Add(data);
            }
        }

        // Ordena as datas para exibição cronológica
        var listaDeDatasOrdenada = datasRelevantes.OrderBy(d => d).ToList();

        // --- Etapa 3: Cálculo Focado (Preservado do seu arquivo, mas agora itera sobre a lista filtrada) ---
        var pontosDeDados = new List<PontoDeDadosDiario>();
        foreach (var data in listaDeDatasOrdenada)
        {
            var capacidadeDoDiaPorRecurso = new Dictionary<string, decimal>();
            foreach (var recurso in recursos)
            {
                var calendario = await _calendarioRepo.GetByIdAsync(recurso.CalendarioId);
                decimal minutosDisponiveis = calendario?.CalcularHorasDisponiveis(data) * 60 ?? 0;
                capacidadeDoDiaPorRecurso[recurso.Id] = minutosDisponiveis;
            }

            var ordensDoDia = todasAsOrdens.Where(o => DateOnly.FromDateTime(o.DataEntrega) == data);
            decimal demandaDoDiaEmMinutos = _calculadoraDeCarga.Calcular(ordensDoDia);

            pontosDeDados.Add(new PontoDeDadosDiario(data, capacidadeDoDiaPorRecurso, demandaDoDiaEmMinutos));
        }

        return new DadosDashboardResult
        {
            PontosDeDados = pontosDeDados,
            OrdensNoHorizonte = todasAsOrdens.Where(o => DateOnly.FromDateTime(o.DataEntrega) >= request.DataInicio && DateOnly.FromDateTime(o.DataEntrega) <= request.DataFim).ToList(),
            CapacidadeTotalGeral = pontosDeDados.Sum(p => p.CapacidadePorRecurso.Values.Sum()),
            DemandaTotalGeral = pontosDeDados.Sum(p => p.DemandaTotal)
        };
    }
}