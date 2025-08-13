using Application.DTOs; // Será resolvido após corrigir os namespaces no handler
using MediatR;
using System.Collections.Generic;

// Namespace ajustado para corresponder à estrutura do seu projeto
namespace APSSystem.Application.UseCases.AnalisarResultadoGams
{
    /// <summary>
    /// Comando para solicitar a análise de um arquivo de resultado do GAMS.
    /// </summary>
    public class AnalisarResultadoGamsCommand : IRequest<List<ProducaoDto>>
    {
        /// <summary>
        /// Conteúdo textual completo do arquivo de resultado a ser analisado.
        /// </summary>
        public string ConteudoArquivo { get; set; }
    }
}