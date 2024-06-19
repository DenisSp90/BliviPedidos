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

        [MinLength(5, ErrorMessage = "Nome deve ter no minimo 5 caracteres")]
        [MaxLength(50, ErrorMessage = "Nome deve ter no minimo 50 caracteres")]
        [Required(ErrorMessage = "Nome é obrigat�rio")]
        public string Nome { get; set; } = "";

        [Required(ErrorMessage = "E-mail é obrigat�rio")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Telefone é obrigat�rio")]
        public string Telefone { get; set; } = "";

        [Required(ErrorMessage = "Endere�o é obrigat�rio")]
        public string Endereco { get; set; } = "";

        public string Complemento { get; set; } = "";

        [Required(ErrorMessage = "Bairro é obrigat�rio")]
        public string Bairro { get; set; } = "";

        [Required(ErrorMessage = "Municipio é obrigat�rio")]
        public string Municipio { get; set; } = "";

        [Required(ErrorMessage = "UF é obrigat�rio")]
        public string UF { get; set; } = "";

        [Required(ErrorMessage = "CEP é obrigat�rio")]
        public string CEP { get; set; } = "";

        internal void Update(Cadastro novoCadastro)
        {
            this.Pedido = novoCadastro.Pedido;
            this.Bairro = novoCadastro.Bairro;
            this.CEP = novoCadastro.CEP;
            this.Complemento = novoCadastro.Complemento;
            this.Email = novoCadastro.Email;
            this.Endereco = novoCadastro.Endereco;
            this.Municipio = novoCadastro.Municipio;
            this.Nome = novoCadastro.Nome;
            this.Telefone = novoCadastro.Telefone;
            this.UF = novoCadastro.UF;
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
        public Produto Produto { get; private set; }
        [Required]
        [DataMember]
        public int Quantidade { get; private set; }
        [Required]
        [DataMember]
        public decimal PrecoUnitario { get; private set; }
        [DataMember]
        public decimal Subtotal => Quantidade * PrecoUnitario;

        public ItemPedido()
        {

        }

        public ItemPedido(Pedido pedido, Produto produto, int quantidade, decimal precoUnitario)
        {
            Pedido = pedido;
            Produto = produto;
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
