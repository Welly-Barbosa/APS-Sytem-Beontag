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
        // Passa o mapa customizado apenas para o arquivo que precisa dele
        var planoDeProducao = await ParseFileAsync<PlanoDeProducaoItem, ClassMap<PlanoDeProducaoItem>>(Path.Combine(caminhoPastaJob, "f_plano_csv.put"));
        var composicao = await ParseFileAsync<ComposicaoPadraoCorte, ClassMap<ComposicaoPadraoCorte>>(Path.Combine(caminhoPastaJob, "f_composicao_csv.put"));
        var status = await ParseFileAsync<StatusDeEntrega, StatusDeEntregaMap>(Path.Combine(caminhoPastaJob, "f_status_csv.put"));

        return new GamsOutputData
        {
            PlanoDeProducao = planoDeProducao,
            ComposicaoDosPadroes = composicao,
            StatusDasEntregas = status
        };
    }

    private async Task<List<T>> ParseFileAsync<T, TMap>(string filePath) where TMap : ClassMap
    {
        if (!File.Exists(filePath)) return new List<T>();

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            Delimiter = ",",
        };

        using (var reader = new StreamReader(filePath))
        using (var csv = new CsvReader(reader, config))
        {
            // Registra o mapa de classe específico para este tipo de arquivo
            csv.Context.RegisterClassMap<TMap>();
            var records = csv.GetRecords<T>().ToList();
            return await Task.FromResult(records);
        }
    }
}