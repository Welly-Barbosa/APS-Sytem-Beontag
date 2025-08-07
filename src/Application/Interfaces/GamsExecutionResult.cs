namespace APSSystem.Application.Interfaces;

/// <summary>
/// Representa o resultado de uma execução do processo GAMS.
/// </summary>
/// <param name="Sucesso">Indica se o processo foi concluído sem erros.</param>
/// <param name="CaminhoPastaJob">O caminho para a pasta onde os arquivos de entrada e saída foram gerados.</param>
/// <param name="MensagemErro">Contém a mensagem de erro, se a execução falhar.</param>
public record GamsExecutionResult(
    bool Sucesso,
    string CaminhoPastaJob,
    string? MensagemErro = null);