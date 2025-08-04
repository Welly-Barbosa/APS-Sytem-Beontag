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
        var recursos = (await _recursoRepo.GetAllAsync()).ToList();
        var ordens = (await _ordemRepo.GetAllAsync())
            .Where(o => DateOnly.FromDateTime(o.DataEntrega) >= request.DataInicio && DateOnly.FromDateTime(o.DataEntrega) <= request.DataFim)
            .ToList();

        var pontosDeDados = new List<PontoDeDadosDiario>();
        for (var data = request.DataInicio; data <= request.DataFim; data = data.AddDays(1))
        {
            var capacidadeDoDiaPorRecurso = new Dictionary<string, decimal>();
            foreach (var recurso in recursos)
            {
                var calendario = await _calendarioRepo.GetByIdAsync(recurso.CalendarioId);
                decimal minutosDisponiveis = calendario?.CalcularHorasDisponiveis(data) * 60 ?? 0;
                capacidadeDoDiaPorRecurso[recurso.Id] = minutosDisponiveis;
            }

            var ordensDoDia = ordens.Where(o => DateOnly.FromDateTime(o.DataEntrega) == data);
            decimal demandaDoDiaEmMinutos = _calculadoraDeCarga.Calcular(ordensDoDia);

            pontosDeDados.Add(new PontoDeDadosDiario(data, capacidadeDoDiaPorRecurso, demandaDoDiaEmMinutos));
        }

        return new DadosDashboardResult
        {
            PontosDeDados = pontosDeDados,
            OrdensNoHorizonte = ordens
        };
    }
}