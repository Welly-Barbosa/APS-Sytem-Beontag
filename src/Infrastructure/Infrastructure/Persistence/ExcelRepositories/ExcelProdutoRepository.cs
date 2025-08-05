using APSSystem.Application.Interfaces;
using APSSystem.Core.Entities;
using APSSystem.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace APSSystem.Infrastructure.Persistence.ExcelRepositories;

public class ExcelProdutoRepository : IProdutoRepository
{
    private readonly IExcelDataService _dataService;

    public ExcelProdutoRepository(IExcelDataService dataService)
    {
        _dataService = dataService;
    }

    public Task<IEnumerable<Produto>> GetAllAsync()
    {
        var produtos = new List<Produto>();
        var dataTable = _dataService.GetDataTable("Produtos.xlsx");

        foreach (DataRow row in dataTable.Rows)
        {
            try
            {
                var produto = new Produto(
                    PN_Generico: row["PN_Generico"].ToString()!,
                    Descricao: row["Descricao"].ToString()!
                );
                produtos.Add(produto);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao mapear uma linha de PRODUTO: {ex.Message}");
            }
        }
        return Task.FromResult(produtos.AsEnumerable());
    }

    public async Task<Produto?> GetByPNGenericoAsync(string pnGenerico)
    {
        var todosOsProdutos = await GetAllAsync();
        return todosOsProdutos.FirstOrDefault(p => p.PN_Generico.Equals(pnGenerico, StringComparison.OrdinalIgnoreCase));
    }
}