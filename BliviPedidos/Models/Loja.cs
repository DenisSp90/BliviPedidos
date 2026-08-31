using System.ComponentModel.DataAnnotations;

namespace BliviPedidos.Models;

public class Loja
{
    public const int PadraoId = 1;

    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Nome { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Dominio { get; set; }

    [MaxLength(500)]
    public string? LogoUrl { get; set; }

    [MaxLength(20)]
    public string? CorPrimaria { get; set; }

    [MaxLength(20)]
    public string? CorSecundaria { get; set; }

    [MaxLength(600)]
    public string? Descricao { get; set; }

    [MaxLength(20)]
    public string? Whatsapp { get; set; }

    [MaxLength(255)]
    public string? EmailContato { get; set; }

    [MaxLength(500)]
    public string? InstagramUrl { get; set; }

    public bool PixAtivo { get; set; }

    [MaxLength(120)]
    public string? PixResponsavel { get; set; }

    [MaxLength(20)]
    public string? PixTipo { get; set; }

    [MaxLength(255)]
    public string? PixChave { get; set; }

    [MaxLength(80)]
    public string? PixCidade { get; set; }

    public bool Ativa { get; set; } = true;

    public bool RetiradaAtiva { get; set; } = true;

    public bool EntregaAtiva { get; set; }

    [MaxLength(9)]
    public string? CepOrigem { get; set; }

    [MaxLength(180)]
    public string? EnderecoOrigem { get; set; }

    [MaxLength(20)]
    public string? NumeroOrigem { get; set; }

    [MaxLength(80)]
    public string? ComplementoOrigem { get; set; }

    [MaxLength(80)]
    public string? BairroOrigem { get; set; }

    [MaxLength(100)]
    public string? MunicipioOrigem { get; set; }

    [MaxLength(2)]
    public string? UfOrigem { get; set; }

    [Range(-90, 90)]
    public decimal? LatitudeOrigem { get; set; }

    [Range(-180, 180)]
    public decimal? LongitudeOrigem { get; set; }

    [Range(0, 100)]
    public decimal PercentualConsumoEntrega { get; set; } = 2m;

    public DateTime? AssinaturaInicioEm { get; set; }

    public DateTime? AssinaturaTerminoEm { get; set; }

    public ICollection<Produto> Produtos { get; set; } = new List<Produto>();
    public ICollection<Categoria> Categorias { get; set; } = new List<Categoria>();
    public ICollection<Cliente> Clientes { get; set; } = new List<Cliente>();
    public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
    public ICollection<UsuarioLoja> Usuarios { get; set; } = new List<UsuarioLoja>();
    public ICollection<FaixaFreteLoja> FaixasFrete { get; set; } = new List<FaixaFreteLoja>();
}
