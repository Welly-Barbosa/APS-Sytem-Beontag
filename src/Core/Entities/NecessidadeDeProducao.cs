using APSSystem.Core.Enums;
using APSSystem.Core.ValueObjects;
using System;

namespace APSSystem.Core.Entities;

/// <summary>
/// Entidade de ligação que representa a necessidade de produção líquida
/// para uma ordem de cliente específica, garantindo a rastreabilidade.
/// </summary>
public class NecessidadeDeProducao
{
    public Guid Id { get; private set; }
    public string NumeroOrdemOriginal { get; private set; }
    public PartNumber PartNumber { get; private set; }
    public DateTime DataEntrega { get; private set; }
    public int QuantidadeLiquida { get; private set; }
    public StatusNecessidade Status { get; private set; }

    public NecessidadeDeProducao(string numeroOrdemOriginal, PartNumber partNumber, DateTime dataEntrega, int quantidadeLiquida)
    {
        Id = Guid.NewGuid();
        NumeroOrdemOriginal = numeroOrdemOriginal;
        PartNumber = partNumber;
        DataEntrega = dataEntrega;
        QuantidadeLiquida = quantidadeLiquida;
        Status = StatusNecessidade.Pendente;
    }

    public void MarcarComoEnviadaParaGams()
    {
        Status = StatusNecessidade.EnviadaParaGams;
    }
}