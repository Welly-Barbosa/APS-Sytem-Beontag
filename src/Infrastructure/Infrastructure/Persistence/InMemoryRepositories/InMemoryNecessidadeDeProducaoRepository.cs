using APSSystem.Core.Entities;
using APSSystem.Core.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace APSSystem.Infrastructure.Persistence.InMemoryRepositories;

public class InMemoryNecessidadeDeProducaoRepository : INecessidadeDeProducaoRepository
{
    private static readonly List<NecessidadeDeProducao> _necessidades = new();

    public Task AdicionarAsync(NecessidadeDeProducao necessidade)
    {
        _necessidades.Add(necessidade);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<NecessidadeDeProducao>> BuscarPendentesAsync()
    {
        return Task.FromResult(_necessidades.Where(n => n.Status == Core.Enums.StatusNecessidade.Pendente).AsEnumerable());
    }
}