using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace BliviPedidos.Models
{
    [DataContract]
    public class BaseModel
    {
        [DataMember]
        public int Id { get; protected set; }
    }

    public class Cadastro : BaseModel
    {
        public Cadastro()
        {
        }

        public int PedidoId { get; set; }
        public virtual Pedido? Pedido { get; set; }

        public int? ClienteId { get; set; }
        public virtual Cliente? Cliente{ get; set; }

        [MinLength(5, ErrorMessage = "Nome deve ter no minimo 5 caracteres")]
        [MaxLength(50, ErrorMessage = "Nome deve ter no minimo 50 caracteres")]
        [Required(ErrorMessage = "Nome é obrigatório")]
        public string Nome { get; set; } = "";

        [EmailAddress(ErrorMessage = "E-mail inválido")]
        public string? Email { get; set; } = "";

        [Required(ErrorMessage = "Telefone é obrigat?rio")]
        [RegularExpression(@"^55\s\d{2}\s\d{5}-\d{4}$", ErrorMessage = "O telefone deve estar no formato 55 99 99999-9999.")]
        public string Telefone { get; set; } = "";

        [Display(Name = "Responsavel Pedido")]
        public string? ResponsavelCerimar { get; set; } = "";

        [Display(Name = "Turma do Aluno")]
        public string? Turma { get; set; } = "";

        public string? Endereco { get; set; } = "";

        public string? Complemento { get; set; } = "";

        public string? Bairro { get; set; } = "";

        public string? Municipio { get; set; } = "";

        public string? UF { get; set; } = "";

        public string? CEP { get; set; } = "";

        internal void Update(Cadastro novoCadastro)
        {
            this.Pedido = novoCadastro.Pedido;
            this.ClienteId = novoCadastro.ClienteId;
            this.Bairro = novoCadastro.Bairro;
            this.CEP = novoCadastro.CEP;
            this.Complemento = novoCadastro.Complemento;
            this.Email = novoCadastro.Email;
            this.Endereco = novoCadastro.Endereco;
            this.Municipio = novoCadastro.Municipio;
            this.Nome = novoCadastro.Nome;
            this.Telefone = novoCadastro.Telefone;
            this.UF = novoCadastro.UF;
            this.ResponsavelCerimar = novoCadastro.ResponsavelCerimar;
            this.Turma = novoCadastro.Turma;
        }
    }

    [DataContract]
    public class ItemPedido : BaseModel
    {
        [Required]
        [DataMember]
        public Pedido Pedido { get; private set; }  

        [Required]
        [DataMember]
        public Produto Produto { get; set; }  

        [Required]
        [DataMember]
        public int Quantidade { get; set; }

        [Required]
        [DataMember]
        public decimal PrecoUnitario { get; set; }

        [DataMember]
        public decimal Subtotal => Quantidade * PrecoUnitario;

        public int PedidoId { get; set; }  
        public int ProdutoId { get; set; }  

        
        public ItemPedido()
        {
        }

        public ItemPedido(Pedido pedido, Produto produto, int quantidade, decimal precoUnitario)
        {
            Pedido = pedido;
            Produto = produto;
            PedidoId = pedido?.Id ?? 0;  
            ProdutoId = produto?.Id ?? 0;  
            Quantidade = quantidade;
            PrecoUnitario = precoUnitario;
        }

        internal void AtualizaQuantidade(int quantidade)
        {
            Quantidade = quantidade;
        }
    }

    public class Pedido : BaseModel
    {
        public Pedido()
        {
            Cadastro = new Cadastro();
        }

        public Pedido(Cadastro cadastro)
        {
            Cadastro = cadastro;
        }

        public List<ItemPedido> Itens { get; set; } = new List<ItemPedido>();

        [Required]
        public virtual Cadastro Cadastro { get; set; }

        public bool Ativo { get; set; }

        public bool Pago { get; set; }

        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime? DataPedido { get; set; }

        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime? DataPagamento { get; set; }

        public decimal ValorTotalPedido { get; set; }

        [EmailAddress(ErrorMessage = "E-mail em formato inválido.")]
        public string? EmailResponsavel { get; set; } = string.Empty;
    }
}
