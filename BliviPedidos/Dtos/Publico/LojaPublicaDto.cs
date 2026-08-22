namespace BliviPedidos.Dtos.Publico;

public sealed class LojaPublicaDto
{
    public required string Nome { get; init; }
    public required string Slug { get; init; }
    public string? LogoUrl { get; init; }
    public string CorPrimaria { get; init; } = "#4154f1";
    public string CorSecundaria { get; init; } = "#ffffff";
    public string? Descricao { get; init; }
    public string? Whatsapp { get; init; }
    public string? EmailContato { get; init; }
    public string? InstagramUrl { get; init; }
}
