using APSSystem.Application.Interfaces;
using APSSystem.Core.Entities;
using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace APSSystem.Infrastructure.GamsIntegration;

public class GamsOutputParser : IGamsOutputParser
{
    public async Task<GamsOutputData> ParseAsync(string caminhoPastaJob)
    {
        var planoDeProducao = await ParseFileAsync<PlanoDeProducaoItem>(Path.Combine(caminhoPastaJob, "f_plano_csv.put"));
        var composicao = await ParseFileAsync<ComposicaoPadraoCorte>(Path.Combine(caminhoPastaJob, "f_composicao_csv.put"));
        var status = await ParseFileAsync<StatusDeEntrega>(Path.Combine(caminhoPastaJob, "f_status_csv.put"));

        return new GamsOutputData
        {
            PlanoDeProducao = planoDeProducao,
            ComposicaoDosPadroes = composicao,
            StatusDasEntregas = status
        };
    }

    private async Task<List<T>> ParseFileAsync<T>(string filePath)
    {
        // Configuração para o CsvHelper entender o formato do seu arquivo
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true, // A primeira linha é um cabeçalho
            Delimiter = ",",
            // O GAMS gera aspas desnecessárias, vamos ignorá-las
            Mode = CsvMode.NoEscape,
            TrimOptions = TrimOptions.Trim,
            BadDataFound = null // Ignora campos extras se a contagem de colunas não bater
        };

        using (var reader = new StreamReader(filePath))
        using (var csv = new CsvReader(reader, config))
        {
            // O CsvHelper mapeia automaticamente as colunas para as propriedades da classe
            var records = csv.GetRecords<T>().ToList();
            return await Task.FromResult(records);
        }
    }
}