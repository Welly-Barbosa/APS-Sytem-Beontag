using APSSystem.Core.Entities;
using System.Collections.Generic; // Adicionar este using
using System.Threading.Tasks;

namespace APSSystem.Core.Interfaces;

public interface IOrdemClienteRepository
{
    Task<OrdemCliente?> GetByNumeroAsync(string numeroOrdem);
    Task<IEnumerable<OrdemCliente>> GetAllAsync(); // <-- NOVA LINHA
}