using APSSystem.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APSSystem.Core.Interfaces;

public interface IItemDeInventarioRepository
{
    Task<IEnumerable<ItemDeInventario>> GetByPNGenericoAsync(string pnGenerico);
}