using System.Threading.Tasks;

namespace APSSystem.Application.Interfaces;

public interface IGamsFileWriter
{
    Task GerarArquivoDeEntradaAsync(string caminhoArquivo, GamsInputData dadosParaGams);
}