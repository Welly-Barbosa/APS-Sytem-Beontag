namespace APSSystem.Core.DTOs;

/// <summary>
/// Representa o resultado de uma operação de validação de dados.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Indica se a validação foi bem-sucedida.
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Contém a mensagem de erro detalhada, se a validação falhar.
    /// </summary>
    public string ErrorMessage { get; private set; }

    private ValidationResult(bool isValid, string errorMessage)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Cria um resultado de sucesso.
    /// </summary>
    public static ValidationResult Success() => new(true, string.Empty);

    /// <summary>
    /// Cria um resultado de falha com uma mensagem de erro.
    /// </summary>
    public static ValidationResult Failure(string message) => new(false, message);
}