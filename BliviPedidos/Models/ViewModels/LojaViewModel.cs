using System.ComponentModel.DataAnnotations;

namespace BliviPedidos.Models.ViewModels;

public class LojaViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "O nome da loja é obrigatório.")]
    [MaxLength(120)]
    [Display(Name = "Nome")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "O slug é obrigatório.")]
    [MaxLength(80)]
    [RegularExpression("^[A-Za-z0-9]+(?:-[A-Za-z0-9]+)*$", ErrorMessage = "Use apenas letras, números e hífens.")]
    [Display(Name = "Slug da URL")]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(255)]
    [Display(Name = "Domínio personalizado")]
    public string? Dominio { get; set; }

    [MaxLength(500)]
    [Url(ErrorMessage = "Informe uma URL válida para o logo.")]
    [Display(Name = "URL do logo")]
    public string? LogoUrl { get; set; }

    [MaxLength(20)]
    [RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Informe uma cor hexadecimal no formato #RRGGBB.")]
    [Display(Name = "Cor primária")]
    public string CorPrimaria { get; set; } = "#0d6efd";

    [MaxLength(20)]
    [RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Informe uma cor hexadecimal no formato #RRGGBB.")]
    [Display(Name = "Cor secundária")]
    public string CorSecundaria { get; set; } = "#ffffff";

    [MaxLength(600)]
    [Display(Name = "Descrição pública")]
    public string? Descricao { get; set; }

    [MaxLength(20)]
    [Display(Name = "WhatsApp")]
    public string? Whatsapp { get; set; }

    [MaxLength(255)]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [Display(Name = "E-mail de contato")]
    public string? EmailContato { get; set; }

    [MaxLength(500)]
    [Url(ErrorMessage = "Informe uma URL válida.")]
    [Display(Name = "URL do Instagram")]
    public string? InstagramUrl { get; set; }

    [Display(Name = "Loja ativa")]
    public bool Ativa { get; set; } = true;

    [Display(Name = "Permitir retirada na loja")]
    public bool RetiradaAtiva { get; set; } = true;

    [Display(Name = "Permitir entrega pela loja")]
    public bool EntregaAtiva { get; set; }

    [MaxLength(9)]
    [RegularExpression(@"^$|^[0-9]{5}-?[0-9]{3}$", ErrorMessage = "Informe um CEP válido.")]
    [Display(Name = "CEP de origem")]
    public string? CepOrigem { get; set; }

    [MaxLength(180)]
    [Display(Name = "Endereço de origem")]
    public string? EnderecoOrigem { get; set; }

    [MaxLength(20)]
    [Display(Name = "Número")]
    public string? NumeroOrigem { get; set; }

    [MaxLength(80)]
    public string? ComplementoOrigem { get; set; }

    [MaxLength(80)]
    [Display(Name = "Bairro")]
    public string? BairroOrigem { get; set; }

    [MaxLength(100)]
    [Display(Name = "Cidade")]
    public string? MunicipioOrigem { get; set; }

    [MaxLength(2)]
    [RegularExpression(@"^$|^[A-Za-z]{2}$", ErrorMessage = "Informe a UF com duas letras.")]
    [Display(Name = "UF")]
    public string? UfOrigem { get; set; }

    [Range(typeof(decimal), "0", "100", ErrorMessage = "Informe um percentual entre 0 e 100.")]
    [Display(Name = "Percentual de consumo sobre os fretes")]
    public decimal PercentualConsumoEntrega { get; set; } = 2m;

    [DataType(DataType.Date)]
    [Display(Name = "Início da assinatura")]
    public DateTime? AssinaturaInicioEm { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Término da assinatura")]
    public DateTime? AssinaturaTerminoEm { get; set; }
}
