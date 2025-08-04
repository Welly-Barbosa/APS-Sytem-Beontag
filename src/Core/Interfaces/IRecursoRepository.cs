using APSSystem.Core.Entities;
using System.Collections.Generic; // Adicionar este using
using System.Threading.Tasks;

namespace APSSystem.Core.Interfaces;

public interface IRecursoRepository
{
    Task<Recurso?> GetByIdAsync(string id);
    Task<IEnumerable<Recurso>> GetAllAsync(); // <-- NOVA LINHA
}