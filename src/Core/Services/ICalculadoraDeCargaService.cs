using APSSystem.Core.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace APSSystem.Core.Services
{
    /// <summary>
    ///     Serviço responsável por executar o cálculo de carga do APS.
    ///     A implementação concreta deve ser registrada no container de DI
    ///     (ex.: services.AddScoped&lt;ICalculadoraDeCargaService, CalculadoraDeCargaService&gt;()).
    /// </summary>
    public interface ICalculadoraDeCargaService
    {
        /// <summary>
        ///     Executa um cálculo macro de carga (assíncrono), usando parâmetros vigentes.
        /// </summary>
        /// <param name="cancellationToken">Token para cancelamento cooperativo.</param>
        /// <returns>Indicador numérico principal do cálculo (ajuste conforme seu domínio).</returns>
        Task<double> CalcularAsync(CancellationToken cancellationToken = default);

        /// <summary>
        ///     Calcula a alocação total em minutos para as ordens do dia,
        ///     combinando tempo de processamento e setups, baseado nos parâmetros do domínio.
        /// </summary>
        /// <param name="ordensDoDia">Coleção de ordens do dia.</param>
        /// <returns>Tempo total (minutos) de processamento + setups.</returns>
        decimal Calcular(IEnumerable<OrdemCliente> ordensDoDia);
    }
}
