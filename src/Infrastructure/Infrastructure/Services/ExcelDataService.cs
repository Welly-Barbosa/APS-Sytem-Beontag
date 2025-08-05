using APSSystem.Application.Interfaces;
using APSSystem.Core.Enums;
using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;

namespace APSSystem.Infrastructure.Services;

public class ExcelDataService : IExcelDataService
{
    private Dictionary<string, DataTable> _loadedData = new();

    public async Task PreloadScenarioDataAsync(CenarioTipo cenario)
    {
        _loadedData.Clear();
        var scenarioFolderName = cenario.ToString();
        var fileNames = new[] { "Recursos.xlsx", "OrdensDeCliente.xlsx", "Inventario.xlsx", "Produtos.xlsx" };

        foreach (var fileName in fileNames)
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "Data", scenarioFolderName, fileName);
            if (File.Exists(filePath))
            {
                // Usamos Task.Run para garantir que a leitura de disco não bloqueie a UI
                var table = await Task.Run(() => ReadExcelSheetToDataTable(filePath));
                _loadedData[fileName] = table;
            }
        }
    }

    public DataTable GetDataTable(string fileName)
    {
        if (_loadedData.TryGetValue(fileName, out var table))
        {
            return table;
        }
        throw new Exception($"Dados da planilha '{fileName}' não foram pré-carregados ou o arquivo não foi encontrado.");
    }

    private DataTable ReadExcelSheetToDataTable(string filePath)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
        using (var reader = ExcelReaderFactory.CreateReader(stream))
        {
            var result = reader.AsDataSet(new ExcelDataSetConfiguration()
            {
                ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = true }
            });
            return result.Tables[0];
        }
    }
}