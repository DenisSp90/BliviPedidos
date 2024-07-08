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

            modelBuilder.Entity<Produto>().HasKey(t => t.Id);

            modelBuilder.Entity<Pedido>().HasKey(t => t.Id);
            modelBuilder.Entity<Pedido>().HasMany(t => t.Itens)
                .WithOne(t => t.Pedido);

            modelBuilder.Entity<Pedido>().HasOne(t => t.Cadastro)
                .WithOne(t => t.Pedido)
                .HasForeignKey<Cadastro>(t => t.PedidoId)
                .IsRequired();

            modelBuilder.Entity<ItemPedido>().HasKey(t => t.Id);
            modelBuilder.Entity<ItemPedido>().HasOne(t => t.Pedido);
            modelBuilder.Entity<ItemPedido>().HasOne(t => t.Produto);

            modelBuilder.Entity<Cadastro>().HasKey(t => t.Id);
            modelBuilder.Entity<Cadastro>().HasOne(t => t.Pedido);

        }

        public DbSet<BliviPedidos.Models.Produto> Produto { get; set; } = default!;
        public DbSet<BliviPedidos.Models.Cliente> Cliente { get; set; } = default!;

    }
}
