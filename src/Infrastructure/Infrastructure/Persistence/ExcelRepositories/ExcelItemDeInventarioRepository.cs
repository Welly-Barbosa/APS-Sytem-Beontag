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

public class ExcelItemDeInventarioRepository : IItemDeInventarioRepository
{
    private readonly IExcelDataService _dataService;

    public ExcelItemDeInventarioRepository(IExcelDataService dataService)
    {
        _dataService = dataService;
    }

    public async Task<IEnumerable<ItemDeInventario>> GetByPNGenericoAsync(string pnGenerico)
    {
        var todoOInventario = await GetAllAsync();
        return todoOInventario.Where(i => i.PartNumber.PN_Generico.Equals(pnGenerico, StringComparison.OrdinalIgnoreCase));
    }

    // Método auxiliar privado para carregar e mapear todos os itens
    private async Task<IEnumerable<ItemDeInventario>> GetAllAsync()
    {
        var inventario = new List<ItemDeInventario>();
        var dataTable = _dataService.GetDataTable("Inventario.xlsx");

        foreach (DataRow row in dataTable.Rows)
        {
            try
            {
                decimal? comprimento = null;
                if (dataTable.Columns.Contains("Comprimento") && row["Comprimento"] != DBNull.Value)
                {
                    comprimento = Convert.ToDecimal(row["Comprimento"]);
                }

                var partNumber = new PartNumber(
                    PN_Generico: row["PN_Generico"].ToString()!,
                    Largura: Convert.ToDecimal(row["Largura"]),
                    Comprimento: comprimento
                );

                var item = new ItemDeInventario(
                    Id: Guid.TryParse(row["IdItem"].ToString(), out var guid) ? guid : Guid.NewGuid(),
                    PartNumber: partNumber,
                    LoteId: row["LoteId"].ToString()!,
                    QuantidadeDisponivel: Convert.ToDecimal(row["QuantidadeDisponivel"]),
                    ClassificacaoABC: Convert.ToChar(row["ClassificacaoABC"])
                );
                inventario.Add(item);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao mapear uma linha de INVENTÁRIO: {ex.Message}");
            }
        }
        return await Task.FromResult(inventario);
    }
}