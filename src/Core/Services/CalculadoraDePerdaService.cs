using APSSystem.Core.Entities;
using APSSystem.Core.ValueObjects;
using System.Collections.Generic;
using System.Linq;

namespace APSSystem.Core.Services;

public class CalculadoraDePerdaService : ICalculadoraDePerdaService
{
    private readonly ParametrosDeCalculoDeCarga _parametros;

    public CalculadoraDePerdaService(ParametrosDeCalculoDeCarga parametros)
    {
        _parametros = parametros;
    }

    public Dictionary<string, decimal> CalcularPerdaPorPadrao(
        IEnumerable<PlanoDeProducaoItem> plano,
        IEnumerable<ComposicaoPadraoCorte> composicoes)
    {
        var perdaPorPadrao = new Dictionary<string, decimal>();
        var composicaoDict = composicoes.ToLookup(c => c.PadraoCorte);

        var padroesUsados = plano.Select(p => p.PadraoCorte).Distinct();

        foreach (var padrao in padroesUsados)
        {
            var itensProduzidos = composicaoDict[padrao];
            if (!itensProduzidos.Any()) continue;

            // Calcula a área total útil produzida por um padrão
            decimal larguraUtilProduzida = itensProduzidos.Sum(item => item.LarguraProduto * item.QtdPorBobinaMae);

            if (_parametros.LarguraBobinaMaeGams > 0)
            {
                decimal perda = 1 - (larguraUtilProduzida / _parametros.LarguraBobinaMaeGams);
                perdaPorPadrao[padrao] = perda; // Armazena como um decimal (ex: 0.05 para 5%)
            }
        }
        return perdaPorPadrao;
    }
}