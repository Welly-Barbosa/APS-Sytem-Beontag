using APSSystem.Core.Entities;
using APSSystem.Core.ValueObjects;
using System; // Adicionado para Math.Ceiling
using System.Collections.Generic;
using System.Linq;

namespace APSSystem.Core.Services;

public class CalculadoraDeCargaService : ICalculadoraDeCargaService
{
    private readonly ParametrosDeCalculoDeCarga _parametros;

    public CalculadoraDeCargaService(ParametrosDeCalculoDeCarga parametros)
    {
        _parametros = parametros;
    }

    public decimal Calcular(IEnumerable<OrdemCliente> ordensDoDia)
    {
        if (!ordensDoDia.Any())
        {
            return 0;
        }

        // Etapa 1: Calcular a quantidade FRACIONÁRIA de bobinas-mãe
        var bobinasFracionarias = ordensDoDia
            .GroupBy(o => o.ItemRequisitado.Comprimento)
            .ToDictionary(
                g => g.Key ?? 0,
                g => g.Sum(o => (o.LarguraCorte * o.Quantidade) / _parametros.LarguraBobinaMae) * _parametros.FatorDePerda
            );

        // --- CORREÇÃO APLICADA AQUI ---
        // Etapa 2: Arredondar para CIMA para obter o número de bobinas INTEIRAS
        var qtdeBobinas10k = (decimal)Math.Ceiling(bobinasFracionarias.GetValueOrDefault(10000));
        var qtdeBobinas15k = (decimal)Math.Ceiling(bobinasFracionarias.GetValueOrDefault(15000));
        var qtdeTotalBobinas = qtdeBobinas10k + qtdeBobinas15k;

        // Etapa 3: Converter a quantidade de bobinas INTEIRAS em tempo de ocupação
        decimal tempoDeProcessamento = (qtdeBobinas10k * _parametros.TempoProcessamentoBobina10k) +
                                       (qtdeBobinas15k * _parametros.TempoProcessamentoBobina15k);

        decimal tempoDeSetup = qtdeTotalBobinas * _parametros.TempoSetupPorBobina;

        return tempoDeProcessamento + tempoDeSetup;
    }
}