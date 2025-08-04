using APSSystem.Core.Entities;
using APSSystem.Core.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace APSSystem.Infrastructure.Persistence.InMemoryRepositories;

public class InMemoryProdutoRepository : IProdutoRepository
{
    private readonly List<Produto> _produtos = new()
    {
        new Produto("PROD-X", "Produto de Teste X"),
        new Produto("PROD-Y", "Produto de Teste Y"),
    };

    public Task<IEnumerable<Produto>> GetAllAsync()
    {
        return Task.FromResult(_produtos.AsEnumerable());
    }

    public Task<Produto?> GetByPNGenericoAsync(string pnGenerico)
    {
        var produto = _produtos.FirstOrDefault(p => p.PN_Generico == pnGenerico);
        return Task.FromResult(produto);
    }
}