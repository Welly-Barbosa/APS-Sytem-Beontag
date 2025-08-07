using System;
using System.Threading.Tasks;

namespace APSSystem.Application.Interfaces;

/// <summary>
/// Define o contrato para um serviço que gerencia a execução de um modelo GAMS.
/// </summary>
public interface IGamsExecutionService
{
    /// <summary>
    /// Executa um ciclo de otimização completo do GAMS de forma assíncrona.
    /// </summary>
    /// <param name="gamsModelPath">O caminho para o arquivo de modelo .gms principal.</param>
    /// <param name="inputData">Os dados de entrada a serem escritos no arquivo .dat.</param>
    /// <param name="timeout">O tempo máximo de execução antes de tentar uma interrupção graciosa.</param>
    /// <returns>O resultado da execução, contendo o status e o caminho para a pasta de job.</returns>
    Task<GamsExecutionResult> ExecutarAsync(string gamsModelPath, GamsInputData inputData, TimeSpan timeout);
}