using System.Data;
using System.IO;

namespace SeuProjeto.Core.Services // Adapte o namespace conforme sua estrutura
{
    /// <summary>
    /// Define o contrato para serviços que leem dados de arquivos Excel.
    /// </summary>
    public interface IExcelDataService
    {
        /// <summary>
        /// Lê o conteúdo de um arquivo Excel a partir de um stream e o converte em um DataSet.
        /// </summary>
        /// <param name="fileStream">O stream de dados do arquivo Excel a ser lido.</param>
        /// <returns>Um DataSet contendo os dados das planilhas do arquivo.</returns>
        DataSet ReadExcelToDataSet(Stream fileStream);
    }
}