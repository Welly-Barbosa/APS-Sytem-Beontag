using System.Collections.Generic;
using APSSystem.Application.DTOs;
using MediatR;

namespace APSSystem.Application.UseCases.AnalisarResultadoGams
{
    public sealed class AnalisarResultadoGamsCommand : IRequest<List<ProducaoDto>>
    {
        public string ConteudoArquivo { get; }
        public AnalisarResultadoGamsCommand(string conteudoArquivo)
        {
            ConteudoArquivo = conteudoArquivo ?? string.Empty;
        }
    }
}