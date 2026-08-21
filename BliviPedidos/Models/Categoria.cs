using System.ComponentModel.DataAnnotations;

namespace BliviPedidos.Models;

public class Categoria
{
    // Temporariamente opcional para permitir a migracao dos dados existentes.
    public int? LojaId { get; set; }
    public Loja? Loja { get; set; }
    public Categoria()
    {
    }
        
    public Categoria(int id, string nome, bool isAtivo, ICollection<Produto> produtos)
    {
        Id = id;
        Nome = nome;
        IsAtivo = isAtivo;
        Produtos = produtos;
    }

    public int Id { get; set; }    
    public string Nome { get; set; } = string.Empty;
    public bool IsAtivo { get; set; }

    // Relacionamento 1:N (uma categoria para muitos produtos)
    public ICollection<Produto>? Produtos { get; set; } = new List<Produto>();
}
