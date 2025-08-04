using APSSystem.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APSSystem.Core.Interfaces;

public interface IProdutoRepository
{
    // Renomeado para maior clareza, pode ser que já esteja assim
    Task<Produto?> GetByPNGenericoAsync(string pnGenerico);
    Task<IEnumerable<Produto>> GetAllAsync();
}