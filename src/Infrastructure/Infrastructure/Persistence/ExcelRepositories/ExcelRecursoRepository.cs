// Exemplo de refatoração para ExcelRecursoRepository.cs
using APSSystem.Application.Interfaces;
using APSSystem.Core.Entities;
using APSSystem.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace APSSystem.Infrastructure.Persistence.ExcelRepositories;

public class ExcelRecursoRepository : IRecursoRepository
{
    private readonly IExcelDataService _dataService;

    public ExcelRecursoRepository(IExcelDataService dataService)
    {
        _dataService = dataService;
    }

    public Task<IEnumerable<Recurso>> GetAllAsync()
    {
        var recursos = new List<Recurso>();
        // Pede a tabela de dados já carregada para o serviço central
        var dataTable = _dataService.GetDataTable("Recursos.xlsx");

        foreach (DataRow row in dataTable.Rows)
        {
            try
            {
                // A lógica de mapeamento permanece a mesma
                var recurso = new Recurso(
                    Id: row["Id"].ToString()!,
                    Descricao: row["Descricao"].ToString()!,
                    VelocidadePolPorMinuto: Convert.ToDecimal(row["VelocidadePolPorMinuto"]),
                    Eficiencia: Convert.ToDecimal(row["Eficiencia"]),
                    TempoDeSetupEmMinutos: Convert.ToDecimal(row["Setup"]),
                    MaximoCortes: Convert.ToInt32(row["MaximoCortes"]),
                    CustoPorHora: Convert.ToDecimal(row["CustoPorHora"]),
                    CalendarioId: row["CalendarioId"].ToString()!
                );
                recursos.Add(recurso);
            }
            catch (Exception ex)
            {
                // O log de erro também permanece
                Console.WriteLine($"Erro ao mapear uma linha de RECURSO: {ex.Message}");
            }
        }
        return Task.FromResult(recursos.AsEnumerable());
    }

    public async Task<Recurso?> GetByIdAsync(string id)
    {
        var todosOsRecursos = await GetAllAsync();
        return todosOsRecursos.FirstOrDefault(r => r.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }
}