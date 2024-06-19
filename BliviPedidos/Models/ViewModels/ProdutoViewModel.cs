using System.ComponentModel.DataAnnotations;

namespace BliviPedidos.Models.ViewModels
{
    public class ProdutoViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O campo 'Codigo' é obrigatório.")]
        [Display(Name = "Código")]
        public string Codigo { get; set; }

        [Required(ErrorMessage = "O campo 'Nome' é obrigatório.")]
        [Display(Name = "Nome")]
        public string Nome { get; set; }

        [Required(ErrorMessage = "O campo 'Preco' é obrigatório.")]
        [Range(0, double.MaxValue, ErrorMessage = "O campo Preço do produto deve ser um número.")]
        [Display(Name = "Preço")]
        public decimal Preco { get; set; }

        public int Quantidade { get; set; }

        public string? Tamanho { get; set; }

        public string? CodeBar { get; set; }

        public byte[]? Foto { get; set; }

        public string? TipoArquivoFoto { get; set; }
    }
}
