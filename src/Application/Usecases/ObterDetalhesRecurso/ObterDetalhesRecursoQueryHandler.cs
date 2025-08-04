using APSSystem.Core.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace APSSystem.Application.UseCases.ObterDetalhesRecurso;

public class ObterDetalhesRecursoQueryHandler
    : IRequestHandler<ObterDetalhesRecursoQuery, DetalhesRecursoResult>
{
    private readonly IRecursoRepository _recursoRepository;
    private readonly ICalendarioRepository _calendarioRepository;

    public ObterDetalhesRecursoQueryHandler(IRecursoRepository recursoRepository, ICalendarioRepository calendarioRepository)
    {
        _recursoRepository = recursoRepository;
        _calendarioRepository = calendarioRepository;
    }

    public async Task<DetalhesRecursoResult> Handle(ObterDetalhesRecursoQuery request, CancellationToken cancellationToken)
    {
        var recurso = await _recursoRepository.GetByIdAsync(request.RecursoId);
        if (recurso is null)
        {
            return new DetalhesRecursoResult { RecursoEncontrado = false, MensagemErro = "Recurso não encontrado." };
        }

        var calendario = await _calendarioRepository.GetByIdAsync(recurso.CalendarioId);
        if (calendario is null)
        {
            return new DetalhesRecursoResult { RecursoEncontrado = false, MensagemErro = $"Calendário com ID '{recurso.CalendarioId}' não encontrado." };
        }

        // Usando nossa lógica de domínio do Core!
        decimal horasDisponiveis = calendario.CalcularHorasDisponiveis(request.DataParaConsulta);

        return new DetalhesRecursoResult
        {
            RecursoEncontrado = true,
            RecursoId = recurso.Id,
            DescricaoRecurso = recurso.Descricao,
            DataConsultada = request.DataParaConsulta,
            HorasDisponiveisNoDia = horasDisponiveis
        };
    }
}