namespace APSSystem.Core.ValueObjects;
public record PartNumber(
    string PN_Generico, // Note o PascalCase
    decimal Largura,
    decimal? Comprimento = null)
{
    public override string ToString() => Comprimento.HasValue ? $"{PN_Generico}-{Largura}x{Comprimento}" : $"{PN_Generico}-{Largura}";
}