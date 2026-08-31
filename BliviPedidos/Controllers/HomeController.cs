using AutoMapper;
using BliviPedidos.Data;
using BliviPedidos.Models;
using BliviPedidos.Models.ViewModels;
using BliviPedidos.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using BliviPedidos.Seguranca;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace BliviPedidos.Controllers;

[Authorize(Policy = PoliticasAutorizacao.AcessoInterno)]
public class HomeController : Controller
{
    private const int LimiteEstoqueCritico = 5;
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IProdutoService _produtoService;
    private readonly IPedidoService _pedidoService;

    public HomeController(
        ILogger<HomeController> logger, 
        ApplicationDbContext context, 
        IMapper mapper, 
        IProdutoService produtoService, 
        IPedidoService pedidoService)
    {
        _logger = logger;
        _context = context;
        _mapper = mapper;
        _produtoService = produtoService;
        _pedidoService = pedidoService;
    }

    public async Task<IActionResult> Index()
    {
        var produtos = await _context.Produto
            .AsNoTracking()
            .Include(produto => produto.Categoria)
            .Where(produto => produto.IsAtivo)
            .ToListAsync(HttpContext.RequestAborted);

        // O dashboard representa pedidos efetivamente registrados. Carrinhos ainda
        // não confirmados ficam de fora, mas concluídos e cancelados permanecem no histórico.
        var pedidos = _pedidoService.GetListaPedidosRegistrados();
        var movimentacoes = _produtoService.GetMovimentacaoEstoque();

        decimal totalValorPedidos = pedidos.Sum(pedido => pedido.ValorTotalPedido);
        decimal totalValorPedidosPagos = pedidos
            .Where(pedido => pedido.StatusPagamento == StatusPagamento.Pago)
            .Sum(pedido => pedido.ValorTotalPedido);
        decimal valorPedidosNaoPagos = totalValorPedidos - totalValorPedidosPagos;
        var produtosEstoqueCritico = produtos
            .Where(produto => produto.Quantidade <= LimiteEstoqueCritico)
            .OrderBy(produto => produto.Quantidade)
            .ThenBy(produto => produto.Nome)
            .Select(produto => new ProdutoEstoqueCriticoViewModel
            {
                Id = produto.Id,
                Nome = produto.Nome,
                Categoria = produto.Categoria?.Nome ?? "Sem categoria",
                Quantidade = produto.Quantidade
            })
            .ToList();

        var inicioPeriodoVendas = DateTime.UtcNow.AddDays(-30);
        var produtosMaisVendidos = pedidos
            .Where(pedido => pedido.Status != StatusPedido.Cancelado
                && pedido.DataPedido >= inicioPeriodoVendas)
            .SelectMany(pedido => pedido.Itens ?? [])
            .Where(item => item.Produto is not null)
            .GroupBy(item => new { item.Produto.Id, item.Produto.Nome })
            .Select(grupo => new DashboardGraficoItemViewModel
            {
                Rotulo = grupo.Key.Nome,
                Valor = grupo.Sum(item => item.Quantidade)
            })
            .OrderByDescending(item => item.Valor)
            .ThenBy(item => item.Rotulo)
            .Take(10)
            .ToList();

        var estoquePorCategoria = produtos
            .GroupBy(produto => produto.Categoria?.Nome ?? "Sem categoria")
            .Select(grupo => new DashboardGraficoItemViewModel
            {
                Rotulo = grupo.Key,
                Valor = grupo.Sum(produto => Math.Max(0, produto.Quantidade))
            })
            .OrderByDescending(item => item.Valor)
            .ThenBy(item => item.Rotulo)
            .ToList();

        StoreViewModel storeViewModel = new StoreViewModel
        {
            Produtos = produtos,
            Pedidos = pedidos,
            TotalValorPedidos = totalValorPedidos,
            ValorPedidosPagos = totalValorPedidosPagos,
            ValorPedidosNaoPagos = valorPedidosNaoPagos,
            Movimentacoes = movimentacoes,
            QuantidadeProdutosAtivos = produtos.Count,
            QuantidadeProdutosEstoqueCritico = produtosEstoqueCritico.Count,
            CustoTotalEstoque = produtos.Sum(produto => Math.Max(0, produto.Quantidade) * produto.PrecoPago),
            ValorVendaPotencialEstoque = produtos.Sum(produto => Math.Max(0, produto.Quantidade) * produto.PrecoVenda),
            ProdutosEstoqueCritico = produtosEstoqueCritico,
            ProdutosMaisVendidos = produtosMaisVendidos,
            EstoquePorCategoria = estoquePorCategoria
        };

        return View(storeViewModel);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(string errorMessage)
    {
        var model = new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            ErrorMessage = errorMessage // Passe a mensagem de erro para a ViewModel
        };

        return View(model);
    }
}
