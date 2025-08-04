using APSSystem.Core.Entities;
using System.Collections.Generic;

namespace APSSystem.Core.Services;

/// <summary>
/// Define o contrato para um serviço que calcula a carga de ocupação em minutos.
/// </summary>
public interface ICalculadoraDeCargaService
{
    /// <summary>
    /// Calcula a ocupação total em minutos para um conjunto de ordens de cliente.
    /// </summary>
    /// <param name="ordensDoDia">A coleção de ordens a serem calculadas.</param>
    /// <returns>A carga total em minutos.</returns>
    decimal Calcular(IEnumerable<OrdemCliente> ordensDoDia);
}