using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using APSSystem.Core.Entities;
using APSSystem.Core.ValueObjects;
using Microsoft.Extensions.Logging;

namespace APSSystem.Core.Services
{
    /// <summary>
    ///     Implementação do serviço responsável pelo cálculo de carga do APS.
    ///     Os parâmetros são fornecidos via DI a partir do appsettings.json (bind feito no Presentation),
    ///     mantendo o Core puro (sem dependência de IConfiguration).
    /// </summary>
    public sealed class CalculadoraDeCargaService : ICalculadoraDeCargaService
    {
        // Campos privados
        private readonly ILogger<CalculadoraDeCargaService> logger;
        private readonly ParametrosDeCalculoDeCarga parametros;

        // Construtores
        /// <summary>
        ///     Injeta logger e parâmetros do cálculo.
        /// </summary>
        /// <param name="logger">Logger para telemetria do cálculo.</param>
        /// <param name="parametros">Parâmetros vindos do appsettings (via DI).</param>
        public CalculadoraDeCargaService(
            ILogger<CalculadoraDeCargaService> logger,
            ParametrosDeCalculoDeCarga parametros)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.parametros = parametros ?? throw new ArgumentNullException(nameof(parametros));
        }

        // Métodos públicos

        /// <summary>
        ///     Executa o cálculo de carga usando os parâmetros vigentes.
        ///     Mantém padrão assíncrono para futura integração com repositórios.
        /// </summary>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        /// <returns>Valor numérico representando a carga calculada.</returns>
        public async Task<double> CalcularAsync(CancellationToken cancellationToken = default)
        {
            // Mantém o método verdadeiramente assíncrono para futuras I/O async
            await Task.Yield();

            // // Se cancelado antes de iniciar, abandona o cálculo
            if (cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning("Cálculo de carga cancelado antes de iniciar.");
                return 0d;
            }

            // // Validação básica de parâmetros
            if (!parametros.IsValid())
            {
                logger.LogWarning("Parâmetros inválidos detectados em {Service}. Usando valores padrão.",
                    nameof(CalculadoraDeCargaService));
            }

            // // Lógica base (placeholder): calcula carga "macro" com fatores do VO
            var cargaBase = parametros.LimiteMaximo > 0 ? parametros.LimiteMaximo : 100.0;
            var fatorOciosidade = 1.0 - Math.Clamp(parametros.PercentualOciosidadePlanejada / 100.0, 0.0, 1.0);
            var carga = cargaBase * parametros.FatorDeAjuste * fatorOciosidade;

            // // Regra opcional do domínio
            if (parametros.HabilitarRegraX && parametros.TamanhoLotePadrao > 0)
            {
                var incremento = parametros.TamanhoLotePadrao * 0.0001 * cargaBase;
                carga += incremento;
            }

            if (carga < 0) carga = 0;

            logger.LogInformation(
                "Cálculo concluído. Resultado={Carga} | Base={Base} | Fator={Fator} | Ociosidade={Ociosidade}% | RegraX={RegraX} | LotePadrao={Lote}",
                carga,
                cargaBase,
                parametros.FatorDeAjuste,
                parametros.PercentualOciosidadePlanejada,
                parametros.HabilitarRegraX,
                parametros.TamanhoLotePadrao);

            return carga;
        }

        /// <summary>
        ///     Calcula a alocação total em minutos para as ordens do dia,
        ///     combinando tempo de processamento e setups. Todos os coeficientes
        ///     vêm de <see cref="ParametrosDeCalculoDeCarga"/> (injetado via DI).
        /// </summary>
        /// <param name="ordensDoDia">Conjunto de ordens do dia.</param>
        /// <returns>Tempo total (minutos) para processamento + setups.</returns>
        public decimal Calcular(IEnumerable<OrdemCliente> ordensDoDia)
        {
            // // Evita NRE e cálculo sobre coleção vazia
            if (ordensDoDia is null) return 0m;

            // // Filtra nulos e materializa
            var ordens = ordensDoDia
                .Where(o => o is not null && o.ItemRequisitado is not null)
                .ToList();

            if (ordens.Count == 0) return 0m;

            // -----------------------------------------------------------------
            // Etapa 1: calcular a quantidade FRACIONÁRIA de bobinas-mãe por comprimento.
            // Fórmula: soma((LarguraCorte * Quantidade) / LarguraBobinaMae) * FatorDePerda
            // Parâmetros (do VO):
            //  - parametros.LarguraBobinaMae (ex.: em mm)
            //  - parametros.FatorDePerda    (ex.: 1.05 para 5% de perda)
            // -----------------------------------------------------------------
            var bobinasFracionarias = ordens
                .GroupBy(o => o.ItemRequisitado!.Comprimento ?? 0) // agrupa por comprimento (ex.: 10000 / 15000)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        // // Soma de (largura de corte * quantidade) / largura bobina-mãe
                        var soma = g.Sum(o =>
                            (o.LarguraCorte * o.Quantidade) / parametros.LarguraBobinaMae);

                        return soma * parametros.FatorDePerda;
                    });

            // -----------------------------------------------------------------
            // Etapa 2: arredondar para CIMA para obter número de bobinas INTEIRAS
            // (atende os comprimentos 10k e 15k; ajuste se houver mais padrões)
            // -----------------------------------------------------------------
            decimal qtdeBobinas10k = 0m, qtdeBobinas15k = 0m;

            if (bobinasFracionarias.TryGetValue(10000, out var frac10k))
                qtdeBobinas10k = Math.Ceiling(frac10k);

            if (bobinasFracionarias.TryGetValue(15000, out var frac15k))
                qtdeBobinas15k = Math.Ceiling(frac15k);

            var qtdeTotalBobinas = qtdeBobinas10k + qtdeBobinas15k;

            // -----------------------------------------------------------------
            // Etapa 3: converter bobinas inteiras em tempo de ocupação (minutos)
            // Parâmetros (do VO):
            //  - parametros.TempoProcessamentoBobina10k (min/bobina 10k)
            //  - parametros.TempoProcessamentoBobina15k (min/bobina 15k)
            //  - parametros.TempoSetupPorBobina        (min/setup por bobina)
            // -----------------------------------------------------------------
            decimal tempoDeProcessamento =
                (qtdeBobinas10k * parametros.TempoProcessamentoBobina10k) +
                (qtdeBobinas15k * parametros.TempoProcessamentoBobina15k);

            decimal tempoDeSetup = qtdeTotalBobinas * parametros.TempoSetupPorBobina;

            return tempoDeProcessamento + tempoDeSetup;
        }
    }
}
