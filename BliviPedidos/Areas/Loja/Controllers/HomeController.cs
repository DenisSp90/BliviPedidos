using BliviPedidos.Data;
using BliviPedidos.Dtos.Publico;
using BliviPedidos.Models;
using BliviPedidos.Models.ViewModels;
using BliviPedidos.Services.Interfaces;
using BliviPedidos.Seguranca;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BliviPedidos.Areas.Loja.Controllers;

[Area("Loja")]
[AllowAnonymous]
public class HomeController : Controller
{
    private readonly ILojaAtualService _lojaAtualService;
    private readonly ApplicationDbContext _context;
    private readonly ICarrinhoPublicoService _carrinhoService;
    private readonly ICalculadorCarrinhoPublicoService _calculadorCarrinhoService;
    private readonly IDadosConsumidorCheckoutService _dadosConsumidorService;
    private readonly IConfirmacaoCheckoutService _confirmacaoCheckoutService;

    public HomeController(
        ILojaAtualService lojaAtualService,
        ApplicationDbContext context,
        ICarrinhoPublicoService carrinhoService,
        ICalculadorCarrinhoPublicoService calculadorCarrinhoService,
        IDadosConsumidorCheckoutService dadosConsumidorService,
        IConfirmacaoCheckoutService confirmacaoCheckoutService)
    {
        _lojaAtualService = lojaAtualService;
        _context = context;
        _carrinhoService = carrinhoService;
        _calculadorCarrinhoService = calculadorCarrinhoService;
        _dadosConsumidorService = dadosConsumidorService;
        _confirmacaoCheckoutService = confirmacaoCheckoutService;
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

    [HttpGet("/loja/{lojaSlug}/carrinho", Name = "CarrinhoLoja")]
    public async Task<IActionResult> Carrinho(string lojaSlug)
    {
        var loja = await _lojaAtualService.ObterLojaAsync();
        var carrinho = _carrinhoService.Obter(loja.Id);
        var resultado = await _calculadorCarrinhoService.ValidarERecalcularAsync(
            carrinho,
            HttpContext.RequestAborted);

        return View(new CarrinhoPublicoViewModel
        {
            Loja = ProjetarLojaPublica(loja),
            Itens = resultado.Itens,
            Erros = resultado.Erros
        });
    }

    [HttpPost("/loja/{lojaSlug}/carrinho/adicionar", Name = "AdicionarCarrinhoLoja")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(PoliticasRateLimit.CarrinhoPublico)]
    [RequestSizeLimit(4 * 1024)]
    [RequestFormLimits(ValueCountLimit = 4, ValueLengthLimit = 2 * 1024)]
    public async Task<IActionResult> AdicionarCarrinho(string lojaSlug, int produtoId)
    {
        var loja = await _lojaAtualService.ObterLojaAsync();
        var produto = await _context.Produto
            .AsNoTracking()
            .Where(produto => produto.Id == produtoId && produto.IsAtivo && produto.Quantidade > 0)
            .Select(produto => new { produto.Id, produto.Quantidade })
            .SingleOrDefaultAsync(HttpContext.RequestAborted);
        if (produto == null)
        {
            return NotFound();
        }

        var quantidadeAtual = _carrinhoService.Obter(loja.Id).Itens
            .SingleOrDefault(item => item.ProdutoId == produtoId)?.Quantidade ?? 0;
        if (quantidadeAtual >= produto.Quantidade)
        {
            TempData["CarrinhoErro"] = "A quantidade disponível desse produto já está no carrinho.";
            return RedirectToRoute("CarrinhoLoja", new { lojaSlug = loja.Slug });
        }

        _carrinhoService.Adicionar(loja.Id, produtoId);
        TempData["CarrinhoMensagem"] = "Produto adicionado ao carrinho.";
        return RedirectToRoute("CarrinhoLoja", new { lojaSlug = loja.Slug });
    }

    [HttpPost("/loja/{lojaSlug}/carrinho/remover", Name = "RemoverCarrinhoLoja")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(PoliticasRateLimit.CarrinhoPublico)]
    [RequestSizeLimit(4 * 1024)]
    [RequestFormLimits(ValueCountLimit = 4, ValueLengthLimit = 2 * 1024)]
    public async Task<IActionResult> RemoverCarrinho(string lojaSlug, int produtoId)
    {
        var loja = await _lojaAtualService.ObterLojaAsync();
        _carrinhoService.Remover(loja.Id, produtoId);
        return RedirectToRoute("CarrinhoLoja", new { lojaSlug = loja.Slug });
    }

    [HttpGet("/loja/{lojaSlug}/checkout", Name = "CheckoutLoja")]
    public async Task<IActionResult> Checkout(string lojaSlug, bool salvo = false)
    {
        var loja = await _lojaAtualService.ObterLojaAsync();
        var resultado = await ValidarCarrinhoCheckoutAsync(loja.Id);
        if (resultado == null)
        {
            return RedirectToRoute("CarrinhoLoja", new { lojaSlug = loja.Slug });
        }

        var dados = _dadosConsumidorService.Obter(loja.Id) ?? new DadosConsumidorCheckout
        {
            Nome = User.Identity?.IsAuthenticated == true ? User.Identity.Name ?? string.Empty : string.Empty,
            Email = User.Identity?.IsAuthenticated == true ? User.FindFirst("email")?.Value : null
        };

        return View(new CheckoutConsumidorViewModel
        {
            Loja = ProjetarLojaPublica(loja),
            Dados = dados,
            Total = resultado.Itens.Sum(item => item.Subtotal),
            QuantidadeItens = resultado.Itens.Sum(item => item.Quantidade),
            DadosSalvos = salvo
        });
    }

    [HttpPost("/loja/{lojaSlug}/checkout", Name = "SalvarDadosCheckoutLoja")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(PoliticasRateLimit.CheckoutPublico)]
    [RequestSizeLimit(16 * 1024)]
    [RequestFormLimits(ValueCountLimit = 16, ValueLengthLimit = 2 * 1024)]
    public async Task<IActionResult> Checkout(
        string lojaSlug,
        [Bind(Prefix = "Dados")] DadosConsumidorCheckout dados)
    {
        var loja = await _lojaAtualService.ObterLojaAsync();
        var resultado = await ValidarCarrinhoCheckoutAsync(loja.Id);
        if (resultado == null)
        {
            return RedirectToRoute("CarrinhoLoja", new { lojaSlug = loja.Slug });
        }

        if (!ModelState.IsValid)
        {
            return View(new CheckoutConsumidorViewModel
            {
                Loja = ProjetarLojaPublica(loja),
                Dados = dados,
                Total = resultado.Itens.Sum(item => item.Subtotal),
                QuantidadeItens = resultado.Itens.Sum(item => item.Quantidade)
            });
        }

        _dadosConsumidorService.Salvar(loja.Id, dados);
        return RedirectToRoute("CheckoutLoja", new { lojaSlug = loja.Slug, salvo = true });
    }

    private async Task<ResultadoValidacaoCarrinhoPublico?> ValidarCarrinhoCheckoutAsync(int lojaId)
    {
        var carrinho = _carrinhoService.Obter(lojaId);
        if (carrinho.Itens.Count == 0)
        {
            TempData["CarrinhoErro"] = "Adicione produtos antes de iniciar o checkout.";
            return null;
        }

        var resultado = await _calculadorCarrinhoService.ValidarERecalcularAsync(
            carrinho,
            HttpContext.RequestAborted);
        if (!resultado.Valido)
        {
            TempData["CarrinhoErro"] = "Revise os produtos e quantidades antes de continuar.";
            return null;
        }

        return resultado;
    }

    [HttpPost("/loja/{lojaSlug}/checkout/confirmar", Name = "ConfirmarCheckoutLoja")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(PoliticasRateLimit.ConfirmacaoCheckoutPublico)]
    [RequestSizeLimit(4 * 1024)]
    [RequestFormLimits(ValueCountLimit = 2, ValueLengthLimit = 2 * 1024)]
    public async Task<IActionResult> ConfirmarCheckout(string lojaSlug)
    {
        var loja = await _lojaAtualService.ObterLojaAsync();
        var resultado = await _confirmacaoCheckoutService.ConfirmarAsync(
            loja.Id,
            HttpContext.RequestAborted);
        if (!resultado.Sucesso)
        {
            TempData["CheckoutErro"] = resultado.Erro;
            return RedirectToRoute("CheckoutLoja", new { lojaSlug = loja.Slug });
        }

        TempData["PedidoConfirmadoCodigo"] = resultado.CodigoPublico;
        return RedirectToRoute("PedidoConfirmadoLoja", new { lojaSlug = loja.Slug });
    }

    [HttpGet("/loja/{lojaSlug}/checkout/confirmado", Name = "PedidoConfirmadoLoja")]
    public async Task<IActionResult> PedidoConfirmado(string lojaSlug)
    {
        var loja = await _lojaAtualService.ObterLojaAsync();
        if (TempData["PedidoConfirmadoCodigo"] is not string codigoPublico ||
            string.IsNullOrWhiteSpace(codigoPublico))
        {
            return RedirectToRoute("CatalogoLoja", new { lojaSlug = loja.Slug });
        }

        return View(new PedidoConfirmadoViewModel
        {
            Loja = ProjetarLojaPublica(loja),
            CodigoPublico = codigoPublico
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
