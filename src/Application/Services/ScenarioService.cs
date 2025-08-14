using APSSystem.Application.Interfaces;
using APSSystem.Core.Enums;
using APSSystem.Application.Services;

namespace APSSystem.Application.Services;

public class ScenarioService : IScenarioService
{
    public CenarioTipo CenarioAtual { get; private set; } = CenarioTipo.Antecipacao; // Cenário padrão

    public void DefinirCenario(CenarioTipo cenario)
    {
        CenarioAtual = cenario;
    }

    public string ObterNomePastaCenario()
    {
        // Retorna o nome da pasta correspondente ao enum.
        return CenarioAtual.ToString();
    }
}