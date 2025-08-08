using APSSystem.Core.Entities;
using System.Collections.Generic;

namespace APSSystem.Core.Services;

public interface ICalculadoraDePerdaService
{
    Dictionary<string, decimal> CalcularPerdaPorPadrao(
        IEnumerable<PlanoDeProducaoItem> plano,
        IEnumerable<ComposicaoPadraoCorte> composicoes);
}