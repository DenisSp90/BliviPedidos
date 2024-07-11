using AutoMapper;
using BliviPedidos.Data;
using BliviPedidos.Models;
using BliviPedidos.Models.ViewModels;
using BliviPedidos.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace BliviPedidos.Controllers;

[Authorize]
public class HomeController : Controller
{
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

    public IActionResult Index()
    {
        var produtosTask = _produtoService.GetProdutosAsync();
        var produtos = produtosTask.GetAwaiter().GetResult();
        var pedidos = _pedidoService.GetListaPedidosAtivos();

        decimal totalValorPedidos = pedidos.Sum(pedido => pedido.ValorTotalPedido);
        decimal totalValorPedidosPagos = pedidos.Where(pedido => pedido.Pago).Sum(pedido => pedido.ValorTotalPedido);
        decimal valorPedidosNaoPagos = totalValorPedidos - totalValorPedidosPagos;

        StoreViewModel storeViewModel = new StoreViewModel
        {
            Produtos = produtos,
            Pedidos = pedidos,
            TotalValorPedidos = totalValorPedidos,
            ValorPedidosPagos = totalValorPedidosPagos,
            ValorPedidosNaoPagos = valorPedidosNaoPagos
        };

        return View(storeViewModel);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
