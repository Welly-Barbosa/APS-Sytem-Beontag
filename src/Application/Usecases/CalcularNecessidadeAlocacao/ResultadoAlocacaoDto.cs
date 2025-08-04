using System.Collections.Generic;

namespace APSSystem.Application.UseCases.CalcularNecessidadeAlocacao;

/// <summary>
/// DTO para retornar o resultado da alocação para o cliente da aplicação.
/// </summary>
public class ResultadoAlocacaoDto
{
    public bool Sucesso { get; set; }
    public string? MensagemErro { get; set; }      // <-- CORRIGIDO
    public string? NumeroOrdem { get; set; }       // <-- CORRIGIDO
    public int QuantidadeRequisitada { get; set; }
    public int NecessidadeProducao { get; set; }
    public List<string> Alocacoes { get; set; } = new();
}