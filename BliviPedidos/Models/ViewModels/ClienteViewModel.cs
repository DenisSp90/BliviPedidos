using System.ComponentModel.DataAnnotations;

namespace BliviPedidos.Models.ViewModels;

public class ClienteViewModel 
{
    public int Id { get; set; }

    [Display(Name = "Nome")]
    public string Nome { get; set; } = string.Empty;

    [Display(Name = "E-mail")]
    [EmailAddress(ErrorMessage = "E-mail inválido")]
    public string? Email { get; set; } = string.Empty;

    [Display(Name = "Telefone")]
    public string? Telefone { get; set; } = string.Empty;

    public string? Endereco { get; set; } = "";

    public string? Complemento { get; set; } = "";

    public string? Bairro { get; set; } = "";

    public string? Municipio { get; set; } = "";

    public string? UF { get; set; } = "";

    public string? CEP { get; set; } = "";

    public IList<Pedido>? Pedidos { get; set; }

}
