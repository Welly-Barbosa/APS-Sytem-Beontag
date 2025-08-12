using APSSystem.Core.DTOs;
using APSSystem.Core.Entities;
using System.Collections.Generic;
using System.Linq;

namespace APSSystem.Core.Services;

public class DataValidationService : IDataValidationService
{
    public ValidationResult ValidateResourcesAndCalendars(
        IEnumerable<Recurso> recursos,
        IEnumerable<Calendario> calendarios)
    {
        var calendarioIds = calendarios.Select(c => c.Id).ToHashSet();

        foreach (var recurso in recursos)
        {
            if (string.IsNullOrWhiteSpace(recurso.CalendarioId))
            {
                return ValidationResult.Failure($"Erro de Validação: O Recurso '{recurso.Id}' não possui um CalendarioId definido.");
            }

            if (!calendarioIds.Contains(recurso.CalendarioId))
            {
                return ValidationResult.Failure($"Erro de Validação: O CalendarioId '{recurso.CalendarioId}' definido para o Recurso '{recurso.Id}' não corresponde a nenhum calendário existente.");
            }
        }

        // Adicione outras validações aqui no futuro

        return ValidationResult.Success();
    }
}