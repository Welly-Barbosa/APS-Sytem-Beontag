using APSSystem.Core.Entities;
using APSSystem.Core.Interfaces;
using APSSystem.Core.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace APSSystem.Infrastructure.Persistence.InMemoryRepositories;

public class InMemoryOrdemClienteRepository : IOrdemClienteRepository
{
    private readonly List<OrdemCliente> _ordens = new()
    {
        // CORRIGIDO: Adicionado o 5º parâmetro 'LarguraCorte' em cada instância.
        // Para dados em memória, vamos assumir que a LarguraCorte é igual à Largura nominal.
        new OrdemCliente(
            "OC-001",
            new PartNumber("PROD-X", 1200),
            100,
            DateTime.Now.AddDays(20),
            1200m), // <-- LarguraCorte

        new OrdemCliente(
            "OC-002",
            new PartNumber("PROD-Y", 800),
            50,
            DateTime.Now.AddDays(30),
            800m) // <-- LarguraCorte
    };

    public Task<OrdemCliente?> GetByNumeroAsync(string numeroOrdem)
    {
        return Task.FromResult(_ordens.FirstOrDefault(o => o.NumeroOrdem == numeroOrdem));
    }

    public Task<IEnumerable<OrdemCliente>> GetAllAsync()
    {
        return Task.FromResult(_ordens.AsEnumerable());
    }
}