namespace APSSystem.Core.Entities;

/// <summary>
/// Representa uma linha do arquivo de composição (f_composicao_csv.put),
/// detalhando o que um padrão de corte produz.
/// </summary>
public record ComposicaoPadraoCorte
{
    /// <summary>
    /// O identificador do padrão de corte.
    /// </summary>
    public string PadraoCorte { get; set; } = string.Empty;

    /// <summary>
    /// O PN Base do produto gerado.
    /// </summary>
    public string PN_Base { get; set; } = string.Empty;

    /// <summary>
    /// A largura do produto gerado.
    /// </summary>
    public decimal LarguraProduto { get; set; }

    /// <summary>
    /// O comprimento do produto gerado.
    /// </summary>
    public decimal CompProduto { get; set; }

    /// <summary>
    /// A quantidade de produtos finais gerada por bobina-mãe.
    /// </summary>
    public decimal QtdPorBobinaMae { get; set; }
}
