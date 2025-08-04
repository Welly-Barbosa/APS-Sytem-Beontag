using APSSystem.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APSSystem.Core.Interfaces;

public interface INecessidadeDeProducaoRepository
{
    Task AdicionarAsync(NecessidadeDeProducao necessidade);
    Task<IEnumerable<NecessidadeDeProducao>> BuscarPendentesAsync();
}