using System;

namespace APSSystem.Core.Entities;

/// <summary>
/// Representa um lote de produção, um agrupador para rastreabilidade.
/// </summary>
/// <param name="Id">O identificador único do lote.</param>
/// <param name="DataCriacao">A data em que o lote foi criado ou recebido.</param>
public record Lote(
    string Id,
    DateTime DataCriacao);