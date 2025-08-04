using APSSystem.Core.ValueObjects;

namespace APSSystem.Core.Entities;

/// <summary>
/// Representa a definição ou "template" de um produto.
/// O inventário físico é gerenciado pela entidade ItemDeInventario.
/// </summary>
/// <param name="PN_Generico">O código ou SKU base do produto.</param>
/// <param name="Descricao">A descrição legível do produto.</param>
public record Produto(
    string PN_Generico,
    string Descricao
);