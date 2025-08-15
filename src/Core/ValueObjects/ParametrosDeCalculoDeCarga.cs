namespace APSSystem.Core.ValueObjects
{
    /// <summary>
    ///     Value Object com os parâmetros que controlam o cálculo de carga.
    ///     Este objeto é imutável do ponto de vista do domínio (configurado na composição da aplicação)
    ///     e deve ser injetado via DI já populado a partir do appsettings.json.
    ///     Mantenha o Core desacoplado de infraestrutura: o bind é feito no Presentation.
    /// </summary>
    public sealed class ParametrosDeCalculoDeCarga
    {
        // Propriedades públicas
        // =======================
        // Parâmetros de bobinagem
        // =======================

        /// <summary>
        ///     Largura da bobina-mãe (ex.: em mm). Ex.: 78.74
        /// </summary>
        public decimal LarguraBobinaMae { get; set; } = 0m;

        /// <summary>
        ///     Fator de perda (ex.: 1.05 para 5% de perda).
        /// </summary>
        public decimal FatorDePerda { get; set; } = 1.0m;

        /// <summary>
        ///     Tempo de processamento por bobina de 10k (em minutos).
        /// </summary>
        public decimal TempoProcessamentoBobina10k { get; set; } = 0m;

        /// <summary>
        ///     Tempo de processamento por bobina de 15k (em minutos).
        /// </summary>
        public decimal TempoProcessamentoBobina15k { get; set; } = 0m;

        /// <summary>
        ///     Tempo de setup por bobina (em minutos).
        /// </summary>
        public decimal TempoSetupPorBobina { get; set; } = 0m;

        /// <summary>
        ///     (Opcional) Largura da bobina-mãe usada por integrações GAMS.
        ///     Mantida separada caso haja conversões/unidades distintas.
        /// </summary>
        public decimal LarguraBobinaMaeGams { get; set; } = 0m;
        /// <summary>
        ///     Limite máximo de carga permitida (por exemplo, em horas ou percentual),
        ///     utilizado como teto no cálculo. Valor padrão 0 indica "não definido".
        /// </summary>
        public double LimiteMaximo { get; set; } = 0.0;

        /// <summary>
        ///     Fator multiplicador aplicado à carga calculada para ajuste fino
        ///     (por exemplo, considerar perdas, setup adicional, etc.). Padrão 1.0.
        /// </summary>
        public double FatorDeAjuste { get; set; } = 1.0;

        /// <summary>
        ///     Percentual de ociosidade planejada (0–100). Ex.: 10 significa reservar 10% de folga.
        /// </summary>
        public double PercentualOciosidadePlanejada { get; set; } = 0.0;

        /// <summary>
        ///     Horizonte de planejamento em dias que o cálculo deve considerar.
        /// </summary>
        public int HorizontePlanejamentoDias { get; set; } = 0;

        /// <summary>
        ///     Tamanho de lote padrão a ser usado quando a informação não vier dos dados de entrada.
        /// </summary>
        public int TamanhoLotePadrao { get; set; } = 0;

        /// <summary>
        ///     Ativa/desativa uma regra adicional do domínio (parametrizável).
        ///     Utilize para feature toggles de heurísticas específicas.
        /// </summary>
        public bool HabilitarRegraX { get; set; } = false;

        // Construtores

        /// <summary>
        ///     Construtor padrão necessário para bind via DI.
        /// </summary>
        public ParametrosDeCalculoDeCarga()
        {
        }

        // Métodos públicos

        /// <summary>
        ///     Validação básica dos parâmetros (opcional).
        ///     Pode ser chamada pela implementação do serviço para garantir consistência.
        /// </summary>
        /// <returns>
        ///     Retorna true se os parâmetros estão dentro de faixas aceitáveis,
        ///     caso contrário false.
        /// </returns>
        public bool IsValid()
        {
            // Regras simples; ajuste conforme o domínio:
            // - LimiteMaximo não pode ser negativo
            // - FatorDeAjuste > 0
            // - PercentualOciosidadePlanejada entre 0 e 100
            // - HorizontePlanejamentoDias >= 0
            // - TamanhoLotePadrao >= 0

            if (LimiteMaximo < 0) return false;
            if (FatorDeAjuste <= 0) return false;
            if (PercentualOciosidadePlanejada < 0 || PercentualOciosidadePlanejada > 100) return false;
            if (HorizontePlanejamentoDias < 0) return false;
            if (TamanhoLotePadrao < 0) return false;

            // Bobinagem
            if (LarguraBobinaMae < 0) return false;
            if (FatorDePerda <= 0) return false;
            if (TempoProcessamentoBobina10k < 0) return false;
            if (TempoProcessamentoBobina15k < 0) return false;
            if (TempoSetupPorBobina < 0) return false;
            if (LarguraBobinaMaeGams < 0) return false;

            // Macro
            if (LimiteMaximo < 0) return false;
            if (FatorDeAjuste <= 0) return false;
            if (PercentualOciosidadePlanejada < 0 || PercentualOciosidadePlanejada > 100) return false;
            if (HorizontePlanejamentoDias < 0) return false;
            if (TamanhoLotePadrao < 0) return false;

            return true;
        }
    }
}
