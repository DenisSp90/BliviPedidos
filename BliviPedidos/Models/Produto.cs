using System.ComponentModel.DataAnnotations;

namespace BliviPedidos.Models
{
    public class Produto
    {
        public Produto()
        {
        }

        public Produto(int id, string codigo, string nome, decimal preco, int quantidade, string tamanho, string codeBar, byte[] foto, string tipoArquivoFoto)
        {
            Id = id;
            Codigo = codigo;
            Nome = nome;
            Preco = preco;
            Quantidade = quantidade;
            Tamanho = tamanho;
            CodeBar = codeBar;
            Foto = foto;
            TipoArquivoFoto = tipoArquivoFoto;
        }

        public int Id { get; protected set; }

        [Required]
        public string Codigo { get; private set; } = string.Empty;
        [Required]
        public string Nome { get; private set; } = string.Empty;
        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "O campo Preço do produto deve ser um número.")]
        public decimal Preco { get; private set; }
        public int Quantidade { get; set; }
        public string? Tamanho { get; set; }
        public string? CodeBar { get; set; }
        public byte[]? Foto { get; set; }
        public string? TipoArquivoFoto { get; set; }
    }
}
