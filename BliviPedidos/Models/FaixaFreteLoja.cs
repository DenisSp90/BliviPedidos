using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BliviPedidos.Models;

public sealed class FaixaFreteLoja
{
    public int Id { get; set; }

    public int LojaId { get; set; }
    public Loja Loja { get; set; } = null!;

    [Column(TypeName = "decimal(8,3)")]
    public decimal DistanciaInicialKm { get; set; }

    [Column(TypeName = "decimal(8,3)")]
    public decimal DistanciaFinalKm { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal ValorFrete { get; set; }

    public bool Ativa { get; set; } = true;

    public int Ordem { get; set; }

    public DateTime CriadaEm { get; set; } = DateTime.UtcNow;

    public DateTime AlteradaEm { get; set; } = DateTime.UtcNow;
}
