using BliviPedidos.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BliviPedidos.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public int LojaIdAtual { get; private set; } = Models.Loja.PadraoId;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Produto>()
                .HasQueryFilter(entidade => entidade.LojaId == LojaIdAtual);
            modelBuilder.Entity<Categoria>()
                .HasQueryFilter(entidade => entidade.LojaId == LojaIdAtual);
            modelBuilder.Entity<Cliente>()
                .HasQueryFilter(entidade => entidade.LojaId == LojaIdAtual);
            modelBuilder.Entity<Pedido>()
                .HasQueryFilter(entidade => entidade.LojaId == LojaIdAtual);
            modelBuilder.Entity<UsuarioLoja>()
                .HasQueryFilter(entidade => entidade.LojaId == LojaIdAtual);
            modelBuilder.Entity<ItemPedido>()
                .HasQueryFilter(entidade => entidade.Pedido.LojaId == LojaIdAtual);
            modelBuilder.Entity<Cadastro>()
                .HasQueryFilter(entidade => entidade.Pedido != null && entidade.Pedido.LojaId == LojaIdAtual);
            modelBuilder.Entity<ProdutoMovimentacao>()
                .HasQueryFilter(entidade => entidade.Produto != null && entidade.Produto.LojaId == LojaIdAtual);

            // Definir a chave primária para Produto
            modelBuilder.Entity<Produto>().HasKey(t => t.Id);

            modelBuilder.Entity<Loja>()
                .HasIndex(l => l.Slug)
                .IsUnique();

            modelBuilder.Entity<Loja>()
                .HasIndex(l => l.Dominio)
                .IsUnique();

            modelBuilder.Entity<Loja>().HasData(new Loja
            {
                Id = 1,
                Nome = "Blivi Pedidos",
                Slug = "blivi-pedidos",
                CorPrimaria = "#0d6efd",
                Ativa = true
            });

            modelBuilder.Entity<Produto>()
                .HasOne(p => p.Loja)
                .WithMany(l => l.Produtos)
                .HasForeignKey(p => p.LojaId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Categoria>()
                .HasOne(c => c.Loja)
                .WithMany(l => l.Categorias)
                .HasForeignKey(c => c.LojaId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Cliente>()
                .HasOne(c => c.Loja)
                .WithMany(l => l.Clientes)
                .HasForeignKey(c => c.LojaId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Pedido>()
                .HasOne(p => p.Loja)
                .WithMany(l => l.Pedidos)
                .HasForeignKey(p => p.LojaId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UsuarioLoja>()
                .HasOne(ul => ul.Usuario)
                .WithOne()
                .HasForeignKey<UsuarioLoja>(ul => ul.UsuarioId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UsuarioLoja>()
                .HasOne(ul => ul.Loja)
                .WithMany(l => l.Usuarios)
                .HasForeignKey(ul => ul.LojaId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            // Definir a chave primária para Categoria
            modelBuilder.Entity<Categoria>().HasKey(c => c.Id);

            // Relacionamento 1:N entre Categoria e Produto
            modelBuilder.Entity<Produto>()
                .HasOne(p => p.Categoria)              // Um Produto tem uma Categoria
                .WithMany(c => c.Produtos)             // Uma Categoria tem muitos Produtos
                .HasForeignKey(p => p.CategoriaId)     // Chave estrangeira em Produto
                .OnDelete(DeleteBehavior.SetNull);     // Se a Categoria for deletada, manter o Produto sem categoria

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

        public void DefinirLojaAtual(int lojaId)
        {
            if (lojaId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lojaId));
            }

            LojaIdAtual = lojaId;
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            AplicarIsolamentoDeLoja();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(
            bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
        {
            AplicarIsolamentoDeLoja();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void AplicarIsolamentoDeLoja()
        {
            foreach (var entry in ChangeTracker.Entries()
                         .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                var lojaId = entry.Entity switch
                {
                    Produto entidade => AjustarLoja(entry.State, entidade.LojaId, id => entidade.LojaId = id),
                    Categoria entidade => AjustarLoja(entry.State, entidade.LojaId, id => entidade.LojaId = id),
                    Cliente entidade => AjustarLoja(entry.State, entidade.LojaId, id => entidade.LojaId = id),
                    Pedido entidade => AjustarLoja(entry.State, entidade.LojaId, id => entidade.LojaId = id),
                    UsuarioLoja entidade => AjustarLoja(entry.State, entidade.LojaId, id => entidade.LojaId = id),
                    _ => LojaIdAtual
                };

                if (lojaId != LojaIdAtual)
                {
                    throw new InvalidOperationException("Não é permitido alterar dados pertencentes a outra loja.");
                }
            }
        }

        private int AjustarLoja(EntityState state, int lojaId, Action<int> definirLoja)
        {
            if (state == EntityState.Added)
            {
                definirLoja(LojaIdAtual);
                return LojaIdAtual;
            }

            return lojaId;
        }

        public DbSet<Categoria> Categoria { get; set; } = default!;
        public DbSet<Loja> Loja { get; set; } = default!;
        public DbSet<Pedido> Pedido { get; set; }
        public DbSet<BliviPedidos.Models.Produto> Produto { get; set; } = default!;
        public DbSet<BliviPedidos.Models.ProdutoMovimentacao> ProdutoMovimentacao { get; set; } = default!;
        public DbSet<BliviPedidos.Models.Cliente> Cliente { get; set; } = default!;
        public DbSet<UsuarioLoja> UsuarioLoja { get; set; } = default!;

    }
}
