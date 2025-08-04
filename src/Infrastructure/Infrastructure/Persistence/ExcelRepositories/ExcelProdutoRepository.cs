using APSSystem.Application.Interfaces;
using APSSystem.Core.Entities;
using APSSystem.Core.Enums;
using APSSystem.Core.Interfaces;
using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace APSSystem.Infrastructure.Persistence.ExcelRepositories;

public class ExcelProdutoRepository : IProdutoRepository
{
    private List<Produto>? _cache;
    private readonly IScenarioService _scenarioService;
    private CenarioTipo _cenarioCache;

    public ExcelProdutoRepository(IScenarioService scenarioService)
    {
        _scenarioService = scenarioService;
    }

    public async Task<IEnumerable<Produto>> GetAllAsync()
    {
        await CarregarDadosSeNecessario();
        return _cache ?? Enumerable.Empty<Produto>();
    }

    public async Task<Produto?> GetByPNGenericoAsync(string pnGenerico)
    {
        await CarregarDadosSeNecessario();
        return _cache?.FirstOrDefault(p => p.PN_Generico.Equals(pnGenerico, StringComparison.OrdinalIgnoreCase));
    }

    private async Task CarregarDadosSeNecessario()
    {
        if (_cache is null || _cenarioCache != _scenarioService.CenarioAtual)
        {
            _cache = await CarregarDadosDoExcel();
            _cenarioCache = _scenarioService.CenarioAtual;
        }
    }

    private async Task<List<Produto>> CarregarDadosDoExcel()
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, "Data", _scenarioService.ObterNomePastaCenario(), "Produtos.xlsx");

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"--- AVISO: Arquivo de produtos não encontrado em: {filePath}");
            return new List<Produto>();
        }

        var produtos = new List<Produto>();
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
        {
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                {
                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = true }
                });

                DataTable dataTable = result.Tables[0];

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
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"--- ERRO ao processar linha de PRODUTO {dataTable.Rows.IndexOf(row) + 2}: {ex.Message}");
                        Console.ResetColor();
                    }
                }
            }
        }
        return await Task.FromResult(produtos);
    }
}