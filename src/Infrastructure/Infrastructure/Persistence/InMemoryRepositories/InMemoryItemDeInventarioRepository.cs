using APSSystem.Core.Entities;
using APSSystem.Core.Interfaces;
using APSSystem.Core.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace APSSystem.Infrastructure.Persistence.InMemoryRepositories;

public class InMemoryItemDeInventarioRepository : IItemDeInventarioRepository
{
    private readonly List<ItemDeInventario> _inventario = new()
    {
        // CORRIGIDO: Adicionado o parâmetro 'ClassificacaoABC'
        new ItemDeInventario(Guid.NewGuid(), new PartNumber("PROD-X", 1200), "LOTE-01", 30, 'A'),
        new ItemDeInventario(Guid.NewGuid(), new PartNumber("PROD-X", 1200), "LOTE-02", 50, 'B'),
        new ItemDeInventario(Guid.NewGuid(), new PartNumber("PROD-X", 1500), "LOTE-03", 100, 'C'),
    };

    public Task<IEnumerable<ItemDeInventario>> GetByPNGenericoAsync(string pnGenerico)
    {
        var itens = _inventario.Where(i => i.PartNumber.PN_Generico == pnGenerico);
        return Task.FromResult(itens);
    }
}