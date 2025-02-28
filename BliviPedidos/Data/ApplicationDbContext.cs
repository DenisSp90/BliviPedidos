using BliviPedidos.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BliviPedidos.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Definir a chave primária para Produto
            modelBuilder.Entity<Produto>().HasKey(t => t.Id);

            // Definir a chave primária para Pedido
            modelBuilder.Entity<Pedido>().HasKey(t => t.Id);
            modelBuilder.Entity<Pedido>()
                .HasMany(t => t.Itens)
                .WithOne(t => t.Pedido)
                .HasForeignKey(t => t.PedidoId); // Relacionamento entre Pedido e ItemPedido

            modelBuilder.Entity<Pedido>()
                .HasOne(t => t.Cadastro) // Relacionamento entre Pedido e Cadastro
                .WithOne(t => t.Pedido)
                .HasForeignKey<Cadastro>(t => t.PedidoId) // Definindo a chave estrangeira para Pedido
                .IsRequired();

            // Definir a chave primária para ItemPedido
            modelBuilder.Entity<ItemPedido>().HasKey(t => t.Id);
            modelBuilder.Entity<ItemPedido>()
                .HasOne(t => t.Pedido) // Relacionamento entre ItemPedido e Pedido
                .WithMany(t => t.Itens)
                .HasForeignKey(t => t.PedidoId) // Definir a chave estrangeira
                .IsRequired();
            modelBuilder.Entity<ItemPedido>()
                .HasOne(t => t.Produto) // Relacionamento entre ItemPedido e Produto
                .WithMany() // Produto pode ter muitos ItemPedidos
                .HasForeignKey(t => t.ProdutoId) // Definir a chave estrangeira
                .IsRequired();

            // Definir a chave primária para Cadastro
            modelBuilder.Entity<Cadastro>().HasKey(t => t.Id);
            modelBuilder.Entity<Cadastro>()
                .HasOne(t => t.Pedido) // Relacionamento entre Cadastro e Pedido
                .WithOne(t => t.Cadastro)
                .HasForeignKey<Cadastro>(t => t.PedidoId);

            // Relacionamento entre Cadastro e Cliente
            modelBuilder.Entity<Cadastro>()
                .HasOne(c => c.Cliente) // Cadastro tem um Cliente
                .WithMany() // Cliente pode ter muitos Cadastros
                .HasForeignKey(c => c.ClienteId) // Relacionamento entre Cadastro e Cliente
                .IsRequired(false); // Defina como necessário, dependendo do seu caso de uso

            // Relacionamento entre ProdutoMovimentacao e Produto
            modelBuilder.Entity<ProdutoMovimentacao>()
                .HasOne(p => p.Produto)
                .WithMany(m => m.ProdutoMovimentacao)
                .HasForeignKey(p => p.ProdutoId);
            
        }




        public DbSet<Pedido> Pedido { get; set; }
        public DbSet<BliviPedidos.Models.Produto> Produto { get; set; } = default!;
        public DbSet<BliviPedidos.Models.ProdutoMovimentacao> ProdutoMovimentacao { get; set; } = default!;
        public DbSet<BliviPedidos.Models.Cliente> Cliente { get; set; } = default!;

    }
}
