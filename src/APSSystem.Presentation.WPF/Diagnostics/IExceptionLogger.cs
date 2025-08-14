using System;

namespace APSSystem.Presentation.WPF.Diagnostics
{
    /// <summary>
    /// Contrato para registrar exceções e mensagens de diagnóstico em arquivo.
    /// </summary>
    public interface IExceptionLogger
    {
        /// <summary>
        /// Grava detalhes completos da exceção (com InnerExceptions) e retorna o caminho do arquivo.
        /// </summary>
        string LogException(Exception ex, string? context = null);

        /// <summary>
        /// Grava uma linha de log informativo/diagnóstico e retorna o caminho do arquivo.
        /// </summary>
        string LogInfo(string message);
    }
}
