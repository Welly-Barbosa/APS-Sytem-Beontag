using APSSystem.Core.Entities;
using APSSystem.Core.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace APSSystem.Infrastructure.Persistence.InMemoryRepositories;

public class InMemoryRecursoRepository : IRecursoRepository
{
    private readonly List<Recurso> _recursos = new()
    {
        new Recurso("MAQ-01", "Máquina de Corte A", 6000m, 0.95m, 30m, 5, 75.5m, "CAL-24x5"), // 6000 in/min, 30 min setup
        new Recurso("MAQ-02", "Máquina de Dobra B", 3000m, 0.98m, 15m, 1, 90.0m, "CAL-PADRAO-5x8"), // 3000 in/min, 15 min setup
    };

    public Task<Recurso?> GetByIdAsync(string id) => Task.FromResult(_recursos.FirstOrDefault(r => r.Id == id));
    public Task<IEnumerable<Recurso>> GetAllAsync() => Task.FromResult(_recursos.AsEnumerable());
}