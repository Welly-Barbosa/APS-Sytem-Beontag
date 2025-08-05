using APSSystem.Application.Interfaces;
using APSSystem.Core.Entities;
using APSSystem.Core.Interfaces;
using APSSystem.Core.ValueObjects;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace APSSystem.Infrastructure.Persistence.ExcelRepositories;

public class ExcelOrdemClienteRepository : IOrdemClienteRepository
{
    private readonly IExcelDataService _dataService;

    public ExcelOrdemClienteRepository(IExcelDataService dataService)
    {
        _dataService = dataService;
    }

    public async Task<IEnumerable<OrdemCliente>> GetAllAsync()
    {
        var ordens = new List<OrdemCliente>();
        // Pede a tabela de dados já carregada para o serviço central
        var dataTable = _dataService.GetDataTable("OrdensDeCliente.xlsx");

        foreach (DataRow row in dataTable.Rows)
        {
            try
            {
                // A lógica de mapeamento permanece a mesma
                var partNumber = new PartNumber(
                    PN_Generico: row["PN_Generico"].ToString()!,
                    Largura: Convert.ToDecimal(row["Largura"]),
                    Comprimento: row["Comprimento"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(row["Comprimento"])
                );

                var ordem = new OrdemCliente(
                    NumeroOrdem: row["NumeroOrdem"].ToString()!,
                    ItemRequisitado: partNumber,
                    Quantidade: Convert.ToInt32(row["Quantidade"]),
                    DataEntrega: Convert.ToDateTime(row["DataEntrega"]),
                    LarguraCorte: Convert.ToDecimal(row["LarguraCorte"])
                );
                ordens.Add(ordem);
            }
            catch (Exception ex)
            {
                // O log de erro agora é mais específico
                Console.WriteLine($"Erro ao mapear uma linha de ORDEM: {ex.Message}");
            }
        }
        return await Task.FromResult(ordens);
    }

    public async Task<OrdemCliente?> GetByNumeroAsync(string numeroOrdem)
    {
        var todasAsOrdens = await GetAllAsync();
        return todasAsOrdens.FirstOrDefault(o => o.NumeroOrdem.Equals(numeroOrdem, StringComparison.OrdinalIgnoreCase));
    }
}