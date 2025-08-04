using APSSystem.Application.Interfaces;
using APSSystem.Core.Entities;
using APSSystem.Core.Enums;
using APSSystem.Core.Interfaces;
using APSSystem.Core.ValueObjects;
using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace APSSystem.Infrastructure.Persistence.ExcelRepositories;

public class ExcelItemDeInventarioRepository : IItemDeInventarioRepository
{
    private List<ItemDeInventario>? _cache;
    private readonly IScenarioService _scenarioService;
    private CenarioTipo _cenarioCache;

    public ExcelItemDeInventarioRepository(IScenarioService scenarioService)
    {
        _scenarioService = scenarioService;
    }

    public async Task<IEnumerable<ItemDeInventario>> GetByPNGenericoAsync(string pnGenerico)
    {
        await CarregarDadosSeNecessario();
        return _cache?.Where(i => i.PartNumber.PN_Generico.Equals(pnGenerico, StringComparison.OrdinalIgnoreCase))
               ?? Enumerable.Empty<ItemDeInventario>();
    }

    private async Task CarregarDadosSeNecessario()
    {
        if (_cache is null || _cenarioCache != _scenarioService.CenarioAtual)
        {
            _cache = await CarregarDadosDoExcel();
            _cenarioCache = _scenarioService.CenarioAtual;
        }
    }

    private async Task<List<ItemDeInventario>> CarregarDadosDoExcel()
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, "Data", _scenarioService.ObterNomePastaCenario(), "Inventario.xlsx");

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"--- AVISO: Arquivo de inventário não encontrado em: {filePath}");
            return new List<ItemDeInventario>();
        }

        var inventario = new List<ItemDeInventario>();
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
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"--- ERRO ao processar linha de INVENTÁRIO {dataTable.Rows.IndexOf(row) + 2}: {ex.Message}");
                        Console.ResetColor();
                    }
                }
            }
        }
        return await Task.FromResult(inventario);
    }
}