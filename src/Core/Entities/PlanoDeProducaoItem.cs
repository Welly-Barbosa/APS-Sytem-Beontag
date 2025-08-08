using System;

namespace APSSystem.Core.Entities;

/// <summary>
/// Representa uma linha do arquivo de plano de produção (f_plano_csv.put), 
/// indicando uma tarefa de produção em uma máquina.
/// </summary>
public record PlanoDeProducaoItem
{
    /// <summary>
    /// A data em que a produção deve ocorrer.
    /// </summary>
    public DateTime DataProducao { get; set; }

    /// <summary>
    /// O ID da máquina que executará a produção.
    /// </summary>
    public string Maquina { get; set; } = string.Empty;

    /// <summary>
    /// O identificador do padrão de corte a ser usado.
    /// </summary>
    public string PadraoCorte { get; set; } = string.Empty;

    /// <summary>
    /// A quantidade de bobinas-mãe a serem consumidas.
    /// </summary>
    public decimal QtdBobinasMae { get; set; }
}