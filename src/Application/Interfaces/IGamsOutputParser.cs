using System.Threading.Tasks;

namespace APSSystem.Application.Interfaces;

/// <summary>
/// Define o contrato para um serviço que lê e interpreta os arquivos de resultado do GAMS.
/// </summary>
public interface IGamsOutputParser
{
    /// <summary>
    /// Lê todos os arquivos de resultado de uma pasta de job específica e os converte em entidades de domínio.
    /// </summary>
    /// <param name="caminhoPastaJob">O caminho completo para a pasta que contém os arquivos .put.</param>
    /// <returns>Um DTO contendo os dados processados.</returns>
    Task<GamsOutputData> ParseAsync(string caminhoPastaJob);
}