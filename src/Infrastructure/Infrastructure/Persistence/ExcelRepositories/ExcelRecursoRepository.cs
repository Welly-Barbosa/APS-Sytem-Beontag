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
                // Função auxiliar para converter com segurança, tratando DBNull
                decimal SafeGetDecimal(string columnName, decimal defaultValue = 0)
                {
                    return row[columnName] == DBNull.Value ? defaultValue : Convert.ToDecimal(row[columnName]);
                }

                int SafeGetInt(string columnName, int defaultValue = 0)
                {
                    return row[columnName] == DBNull.Value ? defaultValue : Convert.ToInt32(row[columnName]);
                }

                var recurso = new Recurso(
                    Id: row["Id"].ToString()!,
                    Descricao: row["Descricao"].ToString()!,
                    VelocidadePolPorMinuto: SafeGetDecimal("VelocidadePolPorMinuto"),
                    Eficiencia: SafeGetDecimal("Eficiencia"),
                    TempoDeSetupEmMinutos: SafeGetDecimal("Setup"),
                    MaximoCortes: SafeGetInt("MaximoCortes"),
                    CustoPorHora: SafeGetDecimal("CustoPorHora"),
                    CalendarioId: row["CalendarioId"].ToString()!
                ); recursos.Add(recurso);
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