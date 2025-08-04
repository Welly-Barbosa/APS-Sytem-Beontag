using APSSystem.Core.ValueObjects;
using System;

namespace APSSystem.Core.Entities;

public record ItemDeInventario(
    Guid Id,
    PartNumber PartNumber,
    string LoteId,
    decimal QuantidadeDisponivel,
    char ClassificacaoABC // <-- CORRIGIDO de Posicao para ClassificacaoABC (char)
);