using APSSystem.Core.DTOs;
using APSSystem.Core.Entities;
using System.Collections.Generic;

namespace APSSystem.Core.Services;

/// <summary>
/// Define o contrato para um serviço que valida a integridade dos dados de entrada.
/// </summary>
public interface IDataValidationService
{
    /// <summary>
    /// Valida a consistência entre recursos e calendários.
    /// </summary>
    ValidationResult ValidateResourcesAndCalendars(
        IEnumerable<Recurso> recursos,
        IEnumerable<Calendario> calendarios);
}