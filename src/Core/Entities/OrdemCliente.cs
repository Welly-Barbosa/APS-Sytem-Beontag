using APSSystem.Core.ValueObjects;
using System;

namespace APSSystem.Core.Entities;

/// <summary>
/// Representa uma ordem de venda de um cliente.
/// </summary>
/// <param name="NumeroOrdem">O identificador único da ordem do cliente.</param>
/// <param name="ItemRequisitado">O PartNumber nominal (de catálogo) solicitado pelo cliente.</param>
/// <param name="Quantidade">A quantidade do produto solicitada.</param>
/// <param name="DataEntrega">A data em que o cliente espera receber o produto.</param>
/// <param name="LarguraCorte">A largura de corte específica para esta ordem, que define o produto a ser otimizado.</param>
public record OrdemCliente(
    string NumeroOrdem,
    PartNumber ItemRequisitado,
    int Quantidade,
    DateTime DataEntrega,
    decimal LarguraCorte // <-- NOVA PROPRIEDADE
);