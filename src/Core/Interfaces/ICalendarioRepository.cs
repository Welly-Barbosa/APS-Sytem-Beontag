using APSSystem.Core.Entities;
using System.Threading.Tasks;

namespace APSSystem.Core.Interfaces;

/// <summary>
/// Define o contrato para operações de acesso a dados para a entidade Calendario.
/// </summary>
public interface ICalendarioRepository
{
    /// <summary>
    /// Busca um calendário pelo seu identificador único.
    /// </summary>
    /// <param name="id">O ID do calendário a ser buscado.</param>
    /// <returns>A entidade Calendario ou nulo se não for encontrada.</returns>
    Task<Calendario?> GetByIdAsync(string id);
}