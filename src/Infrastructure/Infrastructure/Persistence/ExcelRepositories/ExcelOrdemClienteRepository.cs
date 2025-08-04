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

public class ExcelOrdemClienteRepository : IOrdemClienteRepository
{
    private List<OrdemCliente>? _cache;
    private readonly IScenarioService _scenarioService;
    private CenarioTipo _cenarioCache;

    public ExcelOrdemClienteRepository(IScenarioService scenarioService)
    {
        _scenarioService = scenarioService;
    }

    public async Task<OrdemCliente?> GetByNumeroAsync(string numeroOrdem)
    {
        await CarregarDadosSeNecessario();
        return _cache?.FirstOrDefault(o => o.NumeroOrdem.Equals(numeroOrdem, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IEnumerable<OrdemCliente>> GetAllAsync()
    {
        await CarregarDadosSeNecessario();
        return _cache ?? Enumerable.Empty<OrdemCliente>();
    }

    private async Task CarregarDadosSeNecessario()
    {
        if (_cache is null || _cenarioCache != _scenarioService.CenarioAtual)
        {
            _cache = await CarregarDadosDoExcel();
            _cenarioCache = _scenarioService.CenarioAtual;
        }
    }

    private async Task<List<OrdemCliente>> CarregarDadosDoExcel()
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, "Data", _scenarioService.ObterNomePastaCenario(), "OrdensDeCliente.xlsx");

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"--- AVISO: Arquivo de ordens não encontrado em: {filePath}");
            return new List<OrdemCliente>();
        }

        var ordens = new List<OrdemCliente>();
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
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"--- ERRO ao processar linha de ORDEM {dataTable.Rows.IndexOf(row) + 2}: {ex.Message}");
                        Console.ResetColor();
                    }
                }
            }
        }
        return await Task.FromResult(ordens);
    }
}