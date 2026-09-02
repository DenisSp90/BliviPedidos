using System.Security.Claims;
using BliviPedidos.Data;
using BliviPedidos.Models;
using BliviPedidos.Services.Implementations;
using BliviPedidos.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BliviPedidos.Tests;

public class PedidoServiceTests
{
    [Fact]
    public async Task ContadorPendente_DeveIncluirCheckoutEBalcaoEIgnorarCarrinhoFinalizadoECancelado()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        context.Users.AddRange(
            new IdentityUser { Id = "consumidor-1", UserName = "c1@teste.com" },
            new IdentityUser { Id = "consumidor-2", UserName = "c2@teste.com" },
            new IdentityUser { Id = "consumidor-3", UserName = "c3@teste.com" });

        var pedidos = new[]
        {
            CriarPedidoContador(StatusPedido.Carrinho, null, "operador@teste.com"),
            CriarPedidoContador(StatusPedido.Confirmado, "consumidor-1", "cliente@teste.com"),
            CriarPedidoContador(StatusPedido.EmPreparacao, null, "operador@teste.com"),
            CriarPedidoContador(StatusPedido.Pronto, "consumidor-2", "outro@teste.com"),
            CriarPedidoContador(StatusPedido.Concluido, null, "operador@teste.com"),
            CriarPedidoContador(StatusPedido.Cancelado, "consumidor-3", "cliente@teste.com")
        };
        context.Pedido.AddRange(pedidos);
        await context.SaveChangesAsync();
        var accessor = CriarHttpContextAccessor("vendedor@teste.com");
        var service = new PedidoService(
            accessor, context, Mock.Of<IItemPedidoService>(), Mock.Of<ICadastroService>(),
            Mock.Of<IProdutoService>(), accessor, NullLogger<PedidoService>.Instance);

        var quantidade = await service.ContarPedidosPendentesAsync();

        Assert.Equal(3, quantidade);
    }

    [Fact]
    public async Task PedidosRegistrados_DeveContarConcluidosECanceladosMasNaoCarrinhos()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        foreach (var status in new[]
                 {
                     StatusPedido.Carrinho,
                     StatusPedido.Confirmado,
                     StatusPedido.Concluido,
                     StatusPedido.Cancelado
                 })
        {
            context.Pedido.Add(new Pedido
            {
                Status = status,
                EmailResponsavel = status == StatusPedido.Cancelado
                    ? "outro@teste.com"
                    : "vendedor@teste.com",
                Cadastro = new Cadastro
                {
                    Nome = "Cliente teste",
                    Telefone = "55 11 99999-9999"
                }
            });
        }

        await context.SaveChangesAsync();
        var accessor = CriarHttpContextAccessor("vendedor@teste.com");
        var service = new PedidoService(
            accessor, context, Mock.Of<IItemPedidoService>(), Mock.Of<ICadastroService>(),
            Mock.Of<IProdutoService>(), accessor, NullLogger<PedidoService>.Instance);

        var todos = service.GetListaPedidosRegistrados();
        var meus = service.GetListaPedidosRegistradosByEmail("vendedor@teste.com");

        Assert.Equal(3, todos.Count);
        Assert.Contains(todos, pedido => pedido.Status == StatusPedido.Concluido);
        Assert.Contains(todos, pedido => pedido.Status == StatusPedido.Cancelado);
        Assert.Equal(2, meus.Count);
        Assert.DoesNotContain(todos, pedido => pedido.Status == StatusPedido.Carrinho);
    }

    private static Pedido CriarPedidoContador(
        StatusPedido status, string? consumidorUsuarioId, string emailResponsavel) => new()
    {
        Status = status,
        ConsumidorUsuarioId = consumidorUsuarioId,
        EmailResponsavel = emailResponsavel,
        Cadastro = new Cadastro { Nome = "Cliente teste", Telefone = "55 11 99999-9999" }
    };

    [Fact]
    public async Task RegistrarCancelamentoPedido_DeveMarcarCanceladoERestaurarEstoqueUmaVez()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var produto = new Produto
        {
            Nome = "Produto de teste",
            PrecoPago = 5m,
            PrecoVenda = 10m,
            Quantidade = 7,
            IsAtivo = true
        };
        var pedido = new Pedido
        {
            Status = StatusPedido.Confirmado,
            StatusPagamento = StatusPagamento.Pago,
            ValorTotalPedido = 20m,
            Cadastro = new Cadastro
            {
                Nome = "Cliente Teste",
                Telefone = "55 11 99999-9999"
            }
        };
        var item = new ItemPedido(pedido, produto, 2, produto.PrecoVenda);
        pedido.Itens.Add(item);

        context.Add(pedido);
        await context.SaveChangesAsync();

        var produtoService = new Mock<IProdutoService>();

        var httpContextAccessor = CriarHttpContextAccessor("operador@teste.com");
        var service = new PedidoService(
            httpContextAccessor,
            context,
            Mock.Of<IItemPedidoService>(),
            Mock.Of<ICadastroService>(),
            produtoService.Object,
            httpContextAccessor,
            NullLogger<PedidoService>.Instance);

        await service.RegistrarCancelamentoPedido(pedido.Id);

        context.ChangeTracker.Clear();
        var pedidoAtualizado = await context.Pedido
            .Include(p => p.Itens)
            .ThenInclude(i => i.Produto)
            .SingleAsync(p => p.Id == pedido.Id);

        Assert.Equal(StatusPedido.Cancelado, pedidoAtualizado.Status);
        Assert.Equal(StatusPagamento.Cancelado, pedidoAtualizado.StatusPagamento);
        Assert.Equal(0m, pedidoAtualizado.ValorTotalPedido);
        Assert.Equal(9, pedidoAtualizado.Itens.Single().Produto.Quantidade);

        var movimentacaoRegistrada = await context.ProdutoMovimentacao.SingleAsync();
        Assert.Equal(produto.Id, movimentacaoRegistrada.ProdutoId);
        Assert.Equal(2, movimentacaoRegistrada.Quantidade);
        Assert.Equal("Entrada", movimentacaoRegistrada.Tipo);
        Assert.Equal(Loja.PadraoId, movimentacaoRegistrada.LojaId);
        Assert.Equal(pedido.Id, movimentacaoRegistrada.PedidoId);
        Assert.Equal("operador@teste.com", movimentacaoRegistrada.Ator);
        Assert.Equal(OrigemMovimentacaoEstoque.CancelamentoPedido, movimentacaoRegistrada.Origem);
        Assert.Contains("PEDIDO-CANCELAMENTO", movimentacaoRegistrada.Observacao);
        await service.RegistrarCancelamentoPedido(pedido.Id);

        context.ChangeTracker.Clear();
        var pedidoAposSegundaChamada = await context.Pedido
            .Include(p => p.Itens)
            .ThenInclude(i => i.Produto)
            .SingleAsync(p => p.Id == pedido.Id);
        Assert.Equal(9, pedidoAposSegundaChamada.Itens.Single().Produto.Quantidade);
        Assert.Single(await context.ProdutoMovimentacao.ToListAsync());
        produtoService.Verify(
            service => service.RegistrarMovimentacaoAsync(It.IsAny<ProdutoMovimentacao>()),
            Times.Never);
    }

    [Fact]
    public void StatusPedido_DeveConterOCicloExplicitoCompleto()
    {
        Assert.Equal(
            [
                StatusPedido.Carrinho,
                StatusPedido.Confirmado,
                StatusPedido.EmPreparacao,
                StatusPedido.Pronto,
                StatusPedido.Enviado,
                StatusPedido.Concluido,
                StatusPedido.Cancelado
            ],
            Enum.GetValues<StatusPedido>());
    }

    [Fact]
    public async Task AtualizarPagamento_NaoDeveAlterarStatusOperacionalDoPedido()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var pedido = new Pedido
        {
            Status = StatusPedido.EmPreparacao,
            StatusPagamento = StatusPagamento.AguardandoPagamento,
            Cadastro = new Cadastro { Nome = "Cliente", Telefone = "55 11 99999-9999" }
        };
        context.Pedido.Add(pedido);
        await context.SaveChangesAsync();
        var accessor = CriarHttpContextAccessor("operador@teste.com");
        var service = new PedidoService(
            accessor,
            context,
            Mock.Of<IItemPedidoService>(),
            Mock.Of<ICadastroService>(),
            Mock.Of<IProdutoService>(),
            accessor,
            NullLogger<PedidoService>.Instance);

        await service.AtualizarStatusPagamentoAsync(pedido.Id, StatusPagamento.Pago);

        Assert.Equal(StatusPedido.EmPreparacao, pedido.Status);
        Assert.Equal(StatusPagamento.Pago, pedido.StatusPagamento);
        Assert.NotNull(pedido.DataPagamento);
    }

    [Fact]
    public async Task AtualizarStatusPedido_DeveAlterarSomenteSituacaoOperacional()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var pedido = new Pedido
        {
            Status = StatusPedido.Confirmado,
            StatusPagamento = StatusPagamento.Pago,
            Cadastro = new Cadastro { Nome = "Cliente", Telefone = "55 11 99999-9999" }
        };
        context.Pedido.Add(pedido);
        await context.SaveChangesAsync();
        var accessor = CriarHttpContextAccessor("operador@teste.com");
        var service = new PedidoService(
            accessor, context, Mock.Of<IItemPedidoService>(), Mock.Of<ICadastroService>(),
            Mock.Of<IProdutoService>(), accessor, NullLogger<PedidoService>.Instance);

        await service.AtualizarStatusPedidoAsync(pedido.Id, StatusPedido.EmPreparacao);

        Assert.Equal(StatusPedido.EmPreparacao, pedido.Status);
        Assert.Equal(StatusPagamento.Pago, pedido.StatusPagamento);
    }

    [Fact]
    public async Task AtualizarStatusPedido_ParaCancelado_DeveUsarRotinaDeReposicaoDoEstoque()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var produto = new Produto { Nome = "Produto", PrecoVenda = 10m, Quantidade = 3, IsAtivo = true };
        var pedido = new Pedido
        {
            Status = StatusPedido.Confirmado,
            StatusPagamento = StatusPagamento.AguardandoPagamento,
            Cadastro = new Cadastro { Nome = "Cliente", Telefone = "55 11 99999-9999" }
        };
        pedido.Itens.Add(new ItemPedido(pedido, produto, 2, produto.PrecoVenda));
        context.Pedido.Add(pedido);
        await context.SaveChangesAsync();
        var accessor = CriarHttpContextAccessor("operador@teste.com");
        var service = new PedidoService(
            accessor, context, Mock.Of<IItemPedidoService>(), Mock.Of<ICadastroService>(),
            Mock.Of<IProdutoService>(), accessor, NullLogger<PedidoService>.Instance);

        await service.AtualizarStatusPedidoAsync(pedido.Id, StatusPedido.Cancelado);

        context.ChangeTracker.Clear();
        var atualizado = await context.Pedido.Include(p => p.Itens).ThenInclude(i => i.Produto)
            .SingleAsync(p => p.Id == pedido.Id);
        Assert.Equal(StatusPedido.Cancelado, atualizado.Status);
        Assert.Equal(StatusPagamento.Cancelado, atualizado.StatusPagamento);
        Assert.Equal(5, atualizado.Itens.Single().Produto.Quantidade);
        Assert.Single(await context.ProdutoMovimentacao.ToListAsync());
    }

    [Fact]
    public async Task RegistrarCancelamentoPedido_ComPedidoInexistente_DeveFalharSemMovimentarEstoque()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var produtoService = new Mock<IProdutoService>();
        var httpContextAccessor = CriarHttpContextAccessor("operador@teste.com");
        var service = new PedidoService(
            httpContextAccessor,
            context,
            Mock.Of<IItemPedidoService>(),
            Mock.Of<ICadastroService>(),
            produtoService.Object,
            httpContextAccessor,
            NullLogger<PedidoService>.Instance);

        var exception = await Assert.ThrowsAsync<Exception>(
            () => service.RegistrarCancelamentoPedido(999));

        Assert.Equal("Pedido não encontrado", exception.Message);
        produtoService.Verify(
            service => service.RegistrarMovimentacaoAsync(It.IsAny<ProdutoMovimentacao>()),
            Times.Never);
    }

    [Fact]
    public async Task ExcluirPedidoAdministrativamente_ComDevolucao_DeveReporEstoqueEPreservarHistorico()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var produto = new Produto { Nome = "Produto", PrecoVenda = 10m, Quantidade = 4, IsAtivo = true };
        var pedido = new Pedido
        {
            Status = StatusPedido.Confirmado,
            Cadastro = new Cadastro { Nome = "Cliente", Telefone = "55 11 99999-9999" }
        };
        pedido.Itens.Add(new ItemPedido(pedido, produto, 2, produto.PrecoVenda));
        context.Pedido.Add(pedido);
        await context.SaveChangesAsync();
        context.ProdutoMovimentacao.Add(new ProdutoMovimentacao
        {
            ProdutoId = produto.Id,
            PedidoId = pedido.Id,
            Quantidade = 2,
            Tipo = "Saída",
            Ator = "admin@teste.com",
            Origem = OrigemMovimentacaoEstoque.PedidoInterno
        });
        await context.SaveChangesAsync();

        var accessor = CriarHttpContextAccessor("admin@teste.com");
        var service = new PedidoService(
            accessor, context, Mock.Of<IItemPedidoService>(), Mock.Of<ICadastroService>(),
            Mock.Of<IProdutoService>(), accessor, NullLogger<PedidoService>.Instance);

        await service.ExcluirPedidoAdministrativamenteAsync(pedido.Id, devolverEstoque: true);

        context.ChangeTracker.Clear();
        Assert.Empty(await context.Pedido.ToListAsync());
        Assert.Equal(6, (await context.Produto.SingleAsync()).Quantidade);
        var movimentacoes = await context.ProdutoMovimentacao.OrderBy(item => item.Id).ToListAsync();
        Assert.Equal(2, movimentacoes.Count);
        Assert.All(movimentacoes, item => Assert.Null(item.PedidoId));
        Assert.Contains(movimentacoes, item =>
            item.Origem == OrigemMovimentacaoEstoque.ExclusaoAdministrativaPedido
            && item.Tipo == "Entrada"
            && item.Quantidade == 2);
    }

    private static IHttpContextAccessor CriarHttpContextAccessor(string usuario)
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Name, usuario) },
            "TestAuthentication");
        return new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };
    }
}
