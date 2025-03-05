using AutoMapper;
using BliviPedidos.Data;
using BliviPedidos.Models;
using BliviPedidos.Models.ViewModels;
using BliviPedidos.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;

namespace BliviPedidos.Controllers;

[Authorize]
public class RelatorioController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IItemPedidoService _itemPedidoService;
    private readonly IPedidoService _pedidoService;
    private readonly IProdutoService _produtoService;
    private readonly IEmailEnviarService _emailSender;
    private readonly IClienteService _clienteService;
    private readonly IRelatorioService _relatorioService;

    public RelatorioController(
        ApplicationDbContext context,
        IMapper mapper,
        IItemPedidoService itemPedidoService,
        IPedidoService pedidoService,
        IProdutoService produtoService,
        IEmailEnviarService emailSender,
        IClienteService clienteService,
        IRelatorioService relatorioService)
    {
        _context = context;
        _mapper = mapper;
        _itemPedidoService = itemPedidoService;
        _pedidoService = pedidoService;
        _produtoService = produtoService;
        _emailSender = emailSender;
        _clienteService = clienteService;
        _relatorioService = relatorioService;
    }


    public IActionResult Index()
    {
        RelatorioViewModel relatorioViewModel = new();
        relatorioViewModel.Movimentacoes = _produtoService.GetMovimentacaoEstoque();

        return View(relatorioViewModel);
    }

    [Route("Relatorio/ProdutosEmEstoque/{ordenarPor?}/{ordem?}/{filtro?}")]
    public IActionResult ProdutosEmEstoque(string ordenarPor, string ordem, string filtro)
    {
        // Obter os produtos da base de dados
        var produtos = _context.Produto.AsQueryable();

        // Aplicar filtro de status dos produtos
        switch (filtro)
        {
            case "A":
                produtos = produtos.Where(p => p.IsAtivo);
                break;
            case "D":
                produtos = produtos.Where(p => !p.IsAtivo);
                break;            
            default:
                // Não aplica filtro, retorna todos os produtos
                break;
        }

        // Aplicar ordenação
        switch (ordenarPor)
        {
            case "nome":
                produtos = ordem == "asc" ? produtos.OrderBy(p => p.Nome) : produtos.OrderByDescending(p => p.Nome);
                break;
            case "quantidade":
                produtos = ordem == "asc" ? produtos.OrderBy(p => p.Quantidade) : produtos.OrderByDescending(p => p.Quantidade);
                break;
            default: // código
                produtos = ordem == "asc" ? produtos.OrderBy(p => p.Codigo) : produtos.OrderByDescending(p => p.Codigo);
                break;
        }

        // Configurações e geração do relatório
        var tituloRelatorio = "Relatório de Produtos em Estoque";
        var configuracoesRelatorio = new string[] { ordenarPor, ordem, filtro };

        var pdf = _relatorioService.GerarRelatorioProdutosEmEstoque(produtos, tituloRelatorio, configuracoesRelatorio);
        return File(pdf, "application/pdf", "Relatorio_ProdutosEmEstoque.pdf");
    }

    public IActionResult MovimentacaoEstoque()
    {
        var movimentacoes = _produtoService.GetMovimentacaoEstoque();
        var tituloRelatorio = "Relatório de Movimentação do Estoque";

        var pdf = _relatorioService.GerarRelatorioMovimentacaoEstoque(movimentacoes, tituloRelatorio);
        return File(pdf, "application/pdf", "Relatorio_MovimentacaoEstoque.pdf");
    }

    public async Task<IActionResult> ProdutosEmEstoqueByQuantidade(int quantidade)
    {
        var produtos = await _produtoService.GetProdutosEmEstoqueByQuantidadeAsync(quantidade);
        var tituloRelatorio = "Relatório de Produtos com Estoque Baixo";

        var pdf = _relatorioService.GerarRelatorioProdutosComEstoqueBaixo(produtos, tituloRelatorio);
        return File(pdf, "application/pdf", "Relatorio_ProdutosComEstoqueBaixo.pdf");
    }

    public async Task<IActionResult> Pedidos()
    {
        RelatorioViewModel relatorioViewModel = new();
        relatorioViewModel.Pedidos = _pedidoService.GetListaPedidosAtivos();

        return View(relatorioViewModel);
    }

    [Route("Relatorio/PedidosAtivos/{ordenarPor?}/{ordem?}/{filtro?}")]
    public async Task<IActionResult> PedidosAtivos(string ordenarPor, string ordem, string filtro)
    {

    }

}
