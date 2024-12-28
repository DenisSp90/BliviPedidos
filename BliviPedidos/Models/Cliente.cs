using System.ComponentModel.DataAnnotations;

namespace BliviPedidos.Models;

public class Cliente : BaseModel
{
    public Cliente() { }

    public Cliente(string nome, string? email, string? responsavelCerimar, string? turma, string telefone, string? endereco, string? complemento, string? bairro, string? municipio, string? uF, string? cEP, IList<Pedido>? pedidos)
    {
        Nome = nome;
        Email = email;
        ResponsavelCerimar = responsavelCerimar;
        Turma = turma;
        Telefone = telefone;
        Endereco = endereco;
        Complemento = complemento;
        Bairro = bairro;
        Municipio = municipio;
        UF = uF;
        CEP = cEP;
        Pedidos = pedidos;
    }

    public string Nome { get; set; } = "";

    [EmailAddress(ErrorMessage = "E-mail inválido")]
    public string? Email { get; set; } = "";

    public string? ResponsavelCerimar { get; set; } = "";

    public string? Turma { get; set; } = "";

    public string Telefone { get; set; } = "";

    public string? Endereco { get; set; } = "";

    public string? Complemento { get; set; } = "";

    public string? Bairro { get; set; } = "";

    public string? Municipio { get; set; } = "";

    public string? UF { get; set; } = "";

    public string? CEP { get; set; } = "";
    
    public IList<Pedido>? Pedidos { get; set; }

    public int PedidosNaoPagosCount
    {
        get
        {
            return Pedidos.Count(p => !p.Pago);
        }
    }

}
