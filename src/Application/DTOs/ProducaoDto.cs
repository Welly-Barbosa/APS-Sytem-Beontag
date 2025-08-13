using System;

namespace APSSystem.Application.DTOs
{
    /// <summary>
    /// Representa um item de produção (resultado do GAMS) pronto para exibição.
    /// </summary>
    public sealed class ProducaoDto
    {
        /// <summary>
        /// Identifica a linha/recurso/célula de produção.
        /// </summary>
        public string Linha { get; set; } = string.Empty;

        /// <summary>
        /// Código ou nome do produto.
        /// </summary>
        public string Produto { get; set; } = string.Empty;

        /// <summary>
        /// Quantidade planejada/produzida.
        /// </summary>
        public double Quantidade { get; set; }

        /// <summary>
        /// Data/hora de início planejado.
        /// </summary>
        public DateTime? Inicio { get; set; }

        /// <summary>
        /// Data/hora de término planejado.
        /// </summary>
        public DateTime? Fim { get; set; }
    }
}