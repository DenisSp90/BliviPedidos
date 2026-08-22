using BliviPedidos.Data;
using BliviPedidos.Dtos.Publico;
using BliviPedidos.Models.ViewModels;
using BliviPedidos.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BliviPedidos.Areas.Loja.Controllers;

[Area("Loja")]
[AllowAnonymous]
public class HomeController : Controller
{
    private readonly ILojaAtualService _lojaAtualService;
    private readonly ApplicationDbContext _context;

    public HomeController(ILojaAtualService lojaAtualService, ApplicationDbContext context)
    {
        _lojaAtualService = lojaAtualService;
        _context = context;
    }

    [HttpGet("/loja/{lojaSlug}", Name = "CatalogoLoja")]
    public async Task<IActionResult> Index(string lojaSlug, string? busca = null, int? categoriaId = null)
    {
        var loja = await _lojaAtualService.ObterLojaAsync();
        var buscaNormalizada = string.IsNullOrWhiteSpace(busca) ? null : busca.Trim();
        var consultaProdutos = _context.Produto
            .AsNoTracking()
            .Where(produto => produto.IsAtivo && produto.Quantidade > 0);

        if (buscaNormalizada != null)
        {
            consultaProdutos = consultaProdutos.Where(produto =>
                produto.Nome.Contains(buscaNormalizada) ||
                (produto.Codigo != null && produto.Codigo.Contains(buscaNormalizada)));
        }

        if (categoriaId.HasValue)
        {
            consultaProdutos = consultaProdutos.Where(produto => produto.CategoriaId == categoriaId.Value);
        }

        var produtos = await consultaProdutos
            .OrderBy(produto => produto.Nome)
            .Select(ProjetarProdutoPublico())
            .ToListAsync(HttpContext.RequestAborted);

        var categorias = await _context.Categoria
            .AsNoTracking()
            .Where(categoria =>
                categoria.IsAtivo &&
                categoria.Produtos != null &&
                categoria.Produtos.Any(produto => produto.IsAtivo && produto.Quantidade > 0))
            .OrderBy(categoria => categoria.Nome)
            .Select(categoria => new CategoriaPublicaDto
            {
                Id = categoria.Id,
                Nome = categoria.Nome
            })
            .ToListAsync(HttpContext.RequestAborted);

        var model = new CatalogoLojaViewModel
        {
            Loja = ProjetarLojaPublica(loja),
            Produtos = produtos,
            Categorias = categorias,
            Busca = buscaNormalizada,
            CategoriaId = categoriaId
        };

        return View(model);
    }

    [HttpGet("/loja/{lojaSlug}/produto/{id:int}", Name = "DetalheProdutoLoja")]
    public async Task<IActionResult> Produto(string lojaSlug, int id)
    {
        var loja = await _lojaAtualService.ObterLojaAsync();
        var produto = await _context.Produto
            .AsNoTracking()
            .Where(item => item.Id == id && item.IsAtivo && item.Quantidade > 0)
            .Select(ProjetarProdutoPublico())
            .SingleOrDefaultAsync(HttpContext.RequestAborted);

        if (produto == null)
        {
            return NotFound();
        }

        return View(new ProdutoDetalheLojaViewModel
        {
            Loja = ProjetarLojaPublica(loja),
            Produto = produto
        });
    }

    private static LojaPublicaDto ProjetarLojaPublica(BliviPedidos.Models.Loja loja)
    {
        return new LojaPublicaDto
        {
            Nome = loja.Nome,
            Slug = loja.Slug,
            LogoUrl = loja.LogoUrl,
            CorPrimaria = loja.CorPrimaria ?? "#4154f1",
            CorSecundaria = loja.CorSecundaria ?? "#ffffff",
            Descricao = loja.Descricao,
            Whatsapp = loja.Whatsapp,
            EmailContato = loja.EmailContato,
            InstagramUrl = loja.InstagramUrl
        };
    }

    private static System.Linq.Expressions.Expression<Func<BliviPedidos.Models.Produto, ProdutoPublicoDto>>
        ProjetarProdutoPublico()
    {
        return produto => new ProdutoPublicoDto
        {
            Id = produto.Id,
            Codigo = produto.Codigo,
            Nome = produto.Nome,
            PrecoVenda = produto.PrecoVenda,
            Tamanho = produto.Tamanho,
            Foto = produto.Foto,
            CategoriaId = produto.CategoriaId,
            CategoriaNome = produto.Categoria != null ? produto.Categoria.Nome : null
        };
    }
}
