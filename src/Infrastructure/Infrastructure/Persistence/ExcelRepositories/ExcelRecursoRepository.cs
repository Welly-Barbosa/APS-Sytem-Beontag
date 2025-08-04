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

public class ExcelRecursoRepository : IRecursoRepository
{
    // 1. Cache é de instância (não-estático) para suportar múltiplos cenários
    private List<Recurso>? _cache;
    // 2. O serviço de cenário é um campo da classe
    private readonly IScenarioService _scenarioService;
    private CenarioTipo _cenarioCache; // Guarda o cenário para o qual o cache é válido

    // 3. O serviço é injetado no construtor
    public ExcelRecursoRepository(IScenarioService scenarioService)
    {
        _scenarioService = scenarioService;
    }

    public async Task<Recurso?> GetByIdAsync(string id)
    {
        await CarregarDadosSeNecessario();
        return _cache?.FirstOrDefault(r => r.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IEnumerable<Recurso>> GetAllAsync()
    {
        await CarregarDadosSeNecessario();
        return _cache ?? Enumerable.Empty<Recurso>();
    }

    // 4. Nova lógica para carregar/invalidar o cache
    private async Task CarregarDadosSeNecessario()
    {
        // Invalida o cache se ele estiver vazio OU se o cenário na UI mudou
        if (_cache is null || _cenarioCache != _scenarioService.CenarioAtual)
        {
            _cache = await CarregarDadosDoExcel();
            _cenarioCache = _scenarioService.CenarioAtual;
        }
    }

    private async Task<List<Recurso>> CarregarDadosDoExcel()
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, "Data", _scenarioService.ObterNomePastaCenario(), "Recursos.xlsx");

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"--- AVISO: Arquivo de recursos não encontrado em: {filePath}");
            return new List<Recurso>();
        }

        var recursos = new List<Recurso>();
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

                // --- CÓDIGO DE LEITURA DO EXCEL (AGORA COMPLETO) ---
                foreach (DataRow row in dataTable.Rows)
                {
                    try
                    {
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
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"--- ERRO ao processar linha de RECURSO {dataTable.Rows.IndexOf(row) + 2}: {ex.Message}");
                        Console.ResetColor();
                    }
                }
            }
        }
        return await Task.FromResult(recursos);
    }
}