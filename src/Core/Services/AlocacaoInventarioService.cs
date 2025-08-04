using APSSystem.Core.Entities;
using APSSystem.Core.ValueObjects;
using System.Collections.Generic;
using System.Linq;

namespace APSSystem.Core.Services;

/// <summary>
/// Contém a lógica de negócio para alocar itens de inventário para uma ordem.
/// </summary>
public class AlocacaoInventarioService
{
    /// <summary>
    /// Tenta alocar itens do inventário para atender a uma ordem de cliente.
    /// </summary>
    /// <param name="ordem">A ordem do cliente a ser atendida.</param>
    /// <param name="inventarioDisponivel">A lista de itens de inventário candidatos.</param>
    /// <returns>O resultado da alocação.</returns>
    public ResultadoAlocacao Alocar(OrdemCliente ordem, IEnumerable<ItemDeInventario> inventarioDisponivel)
    {
        var necessidadeLiquida = ordem.Quantidade;
        var alocacoes = new List<Alocacao>();

        // Lógica de alocação simplificada para o PoC:
        // 1. Tenta usar itens com PartNumber exato.
        // 2. Poderíamos adicionar lógica para usar "off-cuts" (itens com largura maior).

        var inventarioOrdenado = inventarioDisponivel
            .Where(item => item.PartNumber == ordem.ItemRequisitado)
            .OrderBy(item => item.QuantidadeDisponivel); // Começa com os lotes menores para não "gastar" um grande

        foreach (var item in inventarioOrdenado)
        {
            if (necessidadeLiquida <= 0) break;

            decimal quantidadeAlocada = (decimal)System.Math.Min(necessidadeLiquida, item.QuantidadeDisponivel);

            alocacoes.Add(new Alocacao(item.Id, item.LoteId, quantidadeAlocada));
            necessidadeLiquida -= (int)quantidadeAlocada;
        }

        return new ResultadoAlocacao(necessidadeLiquida, alocacoes);
    }
}

/// <summary>
/// Representa uma alocação específica de um item de inventário.
/// </summary>
public record Alocacao(Guid ItemInventarioId, string LoteId, decimal QuantidadeAlocada);

/// <summary>
/// Representa o resultado completo do processo de alocação.
/// </summary>
public record ResultadoAlocacao(int NecessidadeLiquidaFinal, IReadOnlyCollection<Alocacao> AlocacoesRealizadas);