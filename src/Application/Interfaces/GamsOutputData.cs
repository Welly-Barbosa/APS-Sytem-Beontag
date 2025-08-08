using APSSystem.Core.Entities;
using System.Collections.Generic;

namespace APSSystem.Application.Interfaces;

/// <summary>
/// DTO que agrupa todas as listas de dados lidas dos arquivos de resultado do GAMS.
/// </summary>
public class GamsOutputData
{
    public IReadOnlyList<PlanoDeProducaoItem> PlanoDeProducao { get; init; } = new List<PlanoDeProducaoItem>();
    public IReadOnlyList<ComposicaoPadraoCorte> ComposicaoDosPadroes { get; init; } = new List<ComposicaoPadraoCorte>();
    public IReadOnlyList<StatusDeEntrega> StatusDasEntregas { get; init; } = new List<StatusDeEntrega>();
}