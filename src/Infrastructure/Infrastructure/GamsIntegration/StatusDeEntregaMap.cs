using APSSystem.Core.Entities;
using CsvHelper.Configuration;

namespace APSSystem.Infrastructure.GamsIntegration;

/// <summary>
/// Mapa de classe para o CsvHelper, que define como mapear explicitamente
/// as colunas do arquivo f_status_csv.put para a entidade StatusDeEntrega.
/// </summary>
public sealed class StatusDeEntregaMap : ClassMap<StatusDeEntrega>
{
    public StatusDeEntregaMap()
    {
        // Mapeia cada propriedade para a coluna correspondente no CSV.
        // Força o PN_Base a ser tratado como string para evitar erros de tipo.
        Map(m => m.PN_Base).Name("PN_Base").TypeConverter<CsvHelper.TypeConversion.StringConverter>();
        Map(m => m.LarguraProduto).Name("LarguraProduto");
        Map(m => m.CompProduto).Name("CompProduto");
        Map(m => m.DataEntregaRequerida).Name("DataEntregaRequerida");
        Map(m => m.QtdDemandada).Name("QtdDemandada");
        Map(m => m.DataProducaoReal).Name("DataProducaoReal").TypeConverterOption.NullValues(string.Empty);
        Map(m => m.DiasDesvio).Name("DiasDesvio").TypeConverterOption.NullValues(string.Empty);
        Map(m => m.StatusEntrega).Name("StatusEntrega");
    }
}