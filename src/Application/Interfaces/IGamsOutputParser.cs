using System.Collections.Generic;
using APSSystem.Application.DTOs;

namespace APSSystem.Application.Interfaces
{
    /// <summary>
    /// Define o contrato para interpretar o conteúdo textual de saída do GAMS.
    /// </summary>
    public interface IGamsOutputParser
    {
        /// <summary>
        /// Realiza o parse do conteúdo bruto (texto) retornado pelo GAMS.
        /// </summary>
        /// <param name="conteudo">Conteúdo textual de saída do GAMS.</param>
        /// <returns>Lista de itens de produção.</returns>
        List<ProducaoDto> Parse(string conteudo);
    }
}