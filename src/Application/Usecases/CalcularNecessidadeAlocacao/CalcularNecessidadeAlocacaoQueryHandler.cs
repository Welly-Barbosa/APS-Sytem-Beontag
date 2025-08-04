using APSSystem.Core.Interfaces;
using APSSystem.Core.Services;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace APSSystem.Application.UseCases.CalcularNecessidadeAlocacao;

public class CalcularNecessidadeAlocacaoQueryHandler
    : IRequestHandler<CalcularNecessidadeAlocacaoQuery, ResultadoAlocacaoDto>
{
    private readonly IOrdemClienteRepository _ordemRepo;
    private readonly IItemDeInventarioRepository _inventarioRepo;
    private readonly AlocacaoInventarioService _alocacaoService;

    public CalcularNecessidadeAlocacaoQueryHandler(
        IOrdemClienteRepository ordemRepo,
        IItemDeInventarioRepository inventarioRepo,
        AlocacaoInventarioService alocacaoService)
    {
        _ordemRepo = ordemRepo;
        _inventarioRepo = inventarioRepo;
        _alocacaoService = alocacaoService;
    }

    public async Task<ResultadoAlocacaoDto> Handle(CalcularNecessidadeAlocacaoQuery request, CancellationToken cancellationToken)
    {
        var ordem = await _ordemRepo.GetByNumeroAsync(request.NumeroOrdem);
        if (ordem is null)
        {
            return new ResultadoAlocacaoDto { Sucesso = false, MensagemErro = "Ordem não encontrada." };
        }

        var inventarioDisponivel = await _inventarioRepo.GetByPNGenericoAsync(ordem.ItemRequisitado.PN_Generico);

        // Chama o Serviço de Domínio para executar a lógica complexa
        var resultadoAlocacao = _alocacaoService.Alocar(ordem, inventarioDisponivel);

        // Mapeia o resultado do domínio para um DTO para a camada de apresentação
        return new ResultadoAlocacaoDto
        {
            Sucesso = true,
            NumeroOrdem = ordem.NumeroOrdem,
            QuantidadeRequisitada = ordem.Quantidade,
            NecessidadeProducao = resultadoAlocacao.NecessidadeLiquidaFinal,
            Alocacoes = resultadoAlocacao.AlocacoesRealizadas
                            .Select(a => $"Alocado {a.QuantidadeAlocada} do Lote {a.LoteId}")
                            .ToList()
        };
    }
}