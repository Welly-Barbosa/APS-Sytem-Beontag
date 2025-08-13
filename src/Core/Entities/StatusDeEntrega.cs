using System;

namespace APSSystem.Core.Entities;

/// <summary>
/// Representa uma linha do arquivo de status (f_status_csv.put),
/// mostrando o resultado do planejamento para uma demanda específica.
/// </summary>
public record StatusDeEntrega
{
    public string PN_Base { get; set; } = string.Empty;
    public decimal LarguraProduto { get; set; }
    public decimal CompProduto { get; set; }
    public DateTime DataEntregaRequerida { get; set; }
    public decimal QtdDemandada { get; set; }
    public DateTime? DataProducaoReal { get; set; }
    public int? DiasDesvio { get; set; }
    public int StatusEntrega { get; set; }
}