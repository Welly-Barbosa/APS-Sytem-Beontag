using APSSystem.Core.Entities;
using APSSystem.Core.ValueObjects;
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

        // Etapa 1: Calcular a quantidade de bobinas-mãe para cada tipo de comprimento
        var bobinasPorComprimento = ordensDoDia
            .GroupBy(o => o.ItemRequisitado.Comprimento)
            .ToDictionary(
                g => g.Key ?? 0, // Chave é o comprimento
                g => g.Sum(o => (o.LarguraCorte * o.Quantidade) / _parametros.LarguraBobinaMae) * _parametros.FatorDePerda
            );

        var qtdeBobinas10k = bobinasPorComprimento.GetValueOrDefault(10000);
        var qtdeBobinas15k = bobinasPorComprimento.GetValueOrDefault(15000);
        var qtdeTotalBobinas = qtdeBobinas10k + qtdeBobinas15k;

        // Etapa 2: Converter a quantidade de bobinas em tempo de ocupação
        decimal tempoDeProcessamento = (qtdeBobinas10k * _parametros.TempoProcessamentoBobina10k) +
                                       (qtdeBobinas15k * _parametros.TempoProcessamentoBobina15k);

        decimal tempoDeSetup = qtdeTotalBobinas * _parametros.TempoSetupPorBobina;

        return tempoDeProcessamento + tempoDeSetup;
    }
}