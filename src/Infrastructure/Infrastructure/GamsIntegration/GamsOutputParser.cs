using APSSystem.Application.Interfaces;
using APSSystem.Core.Entities;
using CsvHelper;
using CsvHelper.Configuration;
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
        // Define a configuração padrão para ler os arquivos CSV gerados pelo GAMS
        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true, // A primeira linha é o cabeçalho
            Delimiter = ",",        // O delimitador é a vírgula
            TrimOptions = TrimOptions.Trim, // Remove espaços em branco
            BadDataFound = null     // Ignora erros de contagem de colunas
        };

        var planoDeProducao = await ParseFileAsync<PlanoDeProducaoItem>(Path.Combine(caminhoPastaJob, "f_plano_csv.put"), csvConfig);
        var composicao = await ParseFileAsync<ComposicaoPadraoCorte>(Path.Combine(caminhoPastaJob, "f_composicao_csv.put"), csvConfig);
        var status = await ParseFileAsync<StatusDeEntrega>(Path.Combine(caminhoPastaJob, "f_status_csv.put"), csvConfig);

        return new GamsOutputData
        {
            PlanoDeProducao = planoDeProducao,
            ComposicaoDosPadroes = composicao,
            StatusDasEntregas = status
        };
    }

    private async Task<List<T>> ParseFileAsync<T>(string filePath, CsvConfiguration config)
    {
        if (!File.Exists(filePath))
        {
            // Retorna uma lista vazia se o arquivo de resultado não foi gerado
            return new List<T>();
        }

        using (var reader = new StreamReader(filePath))
        using (var csv = new CsvReader(reader, config))
        {
            // O CsvHelper mapeia automaticamente as colunas do CSV
            // para as propriedades das nossas classes C# (PlanoDeProducaoItem, etc.)
            var records = csv.GetRecords<T>().ToList();
            return await Task.FromResult(records);
        }
    }
}