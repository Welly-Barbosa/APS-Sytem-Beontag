using APSSystem.Core.Enums;

namespace APSSystem.Application.Interfaces;

/// <summary>
/// Define um serviço para gerenciar o cenário de dados ativo na aplicação.
/// </summary>
public interface IScenarioService
{
    /// <summary>
    /// Obtém o cenário de dados atualmente selecionado.
    /// </summary>
    CenarioTipo CenarioAtual { get; }

    /// <summary>
    /// Define o cenário de dados ativo.
    /// </summary>
    /// <param name="cenario">O cenário a ser ativado.</param>
    void DefinirCenario(CenarioTipo cenario);

    /// <summary>
    /// Retorna o nome da pasta para o cenário atual, para ser usado na construção de caminhos de arquivo.
    /// </summary>
    /// <returns>O nome da pasta do cenário.</returns>
    string ObterNomePastaCenario();
}