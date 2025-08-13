using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using APSSystem.Application.DTOs;
using APSSystem.Application.Interfaces;
using MediatR;

namespace APSSystem.Application.UseCases.AnalisarResultadoGams
{
    public sealed class AnalisarResultadoGamsCommandHandler
        : IRequestHandler<AnalisarResultadoGamsCommand, List<ProducaoDto>>
    {
        private readonly IGamsOutputParser _parser;

        public AnalisarResultadoGamsCommandHandler(IGamsOutputParser parser)
        {
            _parser = parser;
        }

        public Task<List<ProducaoDto>> Handle(
            AnalisarResultadoGamsCommand request,
            CancellationToken cancellationToken)
        {
            var resultado = _parser.Parse(request.ConteudoArquivo);
            return Task.FromResult(resultado);
        }
    }
}