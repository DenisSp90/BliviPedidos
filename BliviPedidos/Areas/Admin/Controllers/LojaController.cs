using BliviPedidos.Data;
using BliviPedidos.Models;
using BliviPedidos.Models.ViewModels;
using BliviPedidos.Services.Implementations;
using BliviPedidos.Seguranca;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BliviPedidos.Services.Interfaces;
using LojaModel = BliviPedidos.Models.Loja;

namespace BliviPedidos.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = PoliticasAutorizacao.Administracao)]
public class LojaController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<LojaController> _logger;
    private readonly ICalculadorFreteLojaService _calculadorFrete;

    public LojaController(
        ApplicationDbContext context,
        ILogger<LojaController> logger,
        ICalculadorFreteLojaService calculadorFrete)
    {
        _context = context;
        _logger = logger;
        _calculadorFrete = calculadorFrete;
    }

    public async Task<IActionResult> Index()
    {
        var lojas = await _context.Loja
            .AsNoTracking()
            .OrderByDescending(loja => loja.Ativa)
            .ThenBy(loja => loja.Nome)
            .ToListAsync();

        return View(lojas);
    }

    public IActionResult Criar()
    {
        var inicio = DateTime.Today;
        return View("Formulario", new LojaViewModel
        {
            AssinaturaInicioEm = inicio,
            AssinaturaTerminoEm = inicio.AddYears(1)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(LojaViewModel model)
    {
        Normalizar(model);
        ValidarConfiguracaoEntrega(model);
        if (model.EntregaAtiva)
            ModelState.AddModelError(nameof(model.EntregaAtiva), "Crie a loja, cadastre as faixas de frete e depois ative a entrega.");
        await ValidarUnicidadeAsync(model);

        if (!ModelState.IsValid)
        {
            return View("Formulario", model);
        }

        var loja = new LojaModel();
        Aplicar(model, loja);
        _context.Loja.Add(loja);

        try
        {
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Falha ao criar loja. Slug: {Slug}", model.Slug);
            ModelState.AddModelError(string.Empty, "Não foi possível salvar a loja. Verifique slug e domínio.");
            return View("Formulario", model);
        }
    }

    public async Task<IActionResult> Editar(int id)
    {
        var loja = await _context.Loja.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id);
        if (loja == null)
        {
            return NotFound();
        }

        return View("Formulario", ParaViewModel(loja));
    }

    [HttpGet("/Admin/Loja/{slug}/Pix", Name = "ConfigurarPixLojaAdmin")]
    public async Task<IActionResult> Pix(string slug)
    {
        var loja = await _context.Loja.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Slug == slug);
        if (loja == null)
            return NotFound();

        return View(new LojaPixViewModel
        {
            LojaId = loja.Id,
            LojaNome = loja.Nome,
            Slug = loja.Slug,
            Ativo = loja.PixAtivo,
            Responsavel = loja.PixResponsavel,
            Tipo = loja.PixTipo,
            Chave = loja.PixChave,
            Cidade = loja.PixCidade
        });
    }

    [HttpPost("/Admin/Loja/{slug}/Pix")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pix(string slug, LojaPixViewModel model)
    {
        var loja = await _context.Loja.SingleOrDefaultAsync(item => item.Slug == slug);
        if (loja == null)
            return NotFound();
        if (model.LojaId != loja.Id)
            return BadRequest();

        model.LojaNome = loja.Nome;
        model.Slug = loja.Slug;
        model.Responsavel = NormalizarOpcional(model.Responsavel);
        model.Tipo = NormalizarOpcional(model.Tipo);
        model.Chave = NormalizarOpcional(model.Chave);
        model.Cidade = NormalizarOpcional(model.Cidade)?.ToUpperInvariant();

        var tipos = new[] { "CPF", "CNPJ", "Telefone", "Email", "ChaveAleatoria" };
        if (model.Ativo)
        {
            if (string.IsNullOrWhiteSpace(model.Responsavel))
                ModelState.AddModelError(nameof(model.Responsavel), "Informe o favorecido.");
            if (string.IsNullOrWhiteSpace(model.Chave))
                ModelState.AddModelError(nameof(model.Chave), "Informe a chave PIX.");
            if (string.IsNullOrWhiteSpace(model.Cidade))
                ModelState.AddModelError(nameof(model.Cidade), "Informe a cidade.");
            if (!tipos.Contains(model.Tipo, StringComparer.OrdinalIgnoreCase))
                ModelState.AddModelError(nameof(model.Tipo), "Selecione um tipo de chave válido.");
        }

        if (!ModelState.IsValid)
            return View(model);

        loja.PixAtivo = model.Ativo;
        loja.PixResponsavel = model.Responsavel;
        loja.PixTipo = model.Tipo;
        loja.PixChave = model.Chave;
        loja.PixCidade = model.Cidade;
        await _context.SaveChangesAsync();
        TempData["Sucesso"] = $"Configuração PIX da loja {loja.Nome} atualizada.";
        return RedirectToRoute("ConfigurarPixLojaAdmin", new { slug = loja.Slug });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, LojaViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        Normalizar(model);
        ValidarConfiguracaoEntrega(model);
        if (model.EntregaAtiva && !await _context.FaixaFreteLoja
                .IgnoreQueryFilters()
                .AnyAsync(item => item.LojaId == id && item.Ativa))
            ModelState.AddModelError(nameof(model.EntregaAtiva), "Cadastre ao menos uma faixa de frete ativa antes de ativar a entrega.");
        await ValidarUnicidadeAsync(model);
        if (id == LojaModel.PadraoId && !model.Ativa)
        {
            ModelState.AddModelError(nameof(model.Ativa), "A loja padrão não pode ser desativada.");
        }

        if (!ModelState.IsValid)
        {
            return View("Formulario", model);
        }

        var loja = await _context.Loja.SingleOrDefaultAsync(item => item.Id == id);
        if (loja == null)
        {
            return NotFound();
        }

        Aplicar(model, loja);

        try
        {
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Falha ao editar loja. LojaId: {LojaId}", id);
            ModelState.AddModelError(string.Empty, "Não foi possível salvar a loja. Verifique slug e domínio.");
            return View("Formulario", model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AlterarStatus(int id)
    {
        var loja = await _context.Loja.SingleOrDefaultAsync(item => item.Id == id);
        if (loja == null)
        {
            return NotFound();
        }

        if (loja.Id == LojaModel.PadraoId && loja.Ativa)
        {
            TempData["Erro"] = "A loja padrão não pode ser desativada.";
            return RedirectToAction(nameof(Index));
        }

        loja.Ativa = !loja.Ativa;
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/Admin/Loja/{id:int}/Frete", Name = "ConfigurarFreteLojaAdmin")]
    public async Task<IActionResult> Frete(int id)
    {
        var loja = await _context.Loja.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id);
        if (loja == null)
            return NotFound();

        var faixas = await _context.FaixaFreteLoja
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(item => item.LojaId == id)
            .OrderBy(item => item.Ordem)
            .ThenBy(item => item.DistanciaInicialKm)
            .ToListAsync();

        return View(new ConfiguracaoFreteLojaViewModel
        {
            LojaId = loja.Id,
            LojaNome = loja.Nome,
            LojaSlug = loja.Slug,
            Faixas = faixas,
            NovaFaixa = new FaixaFreteViewModel
            {
                LojaId = loja.Id,
                DistanciaInicialKm = faixas.Where(item => item.Ativa)
                    .Select(item => item.DistanciaFinalKm)
                    .DefaultIfEmpty(0m)
                    .Max()
            }
        });
    }

    [HttpPost("/Admin/Loja/{id:int}/Frete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdicionarFaixaFrete(int id, FaixaFreteViewModel model)
    {
        if (id != model.LojaId)
            return BadRequest();

        var loja = await _context.Loja.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id);
        if (loja == null)
            return NotFound();

        var existentes = await _context.FaixaFreteLoja
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(item => item.LojaId == id)
            .ToListAsync();
        var nova = new FaixaFreteLoja
        {
            LojaId = id,
            DistanciaInicialKm = model.DistanciaInicialKm,
            DistanciaFinalKm = model.DistanciaFinalKm,
            ValorFrete = model.ValorFrete,
            Ativa = model.Ativa,
            Ordem = existentes.Select(item => item.Ordem).DefaultIfEmpty(0).Max() + 1
        };

        var erros = _calculadorFrete.Validar(existentes.Append(nova));
        foreach (var erro in erros)
            ModelState.AddModelError(string.Empty, erro);

        if (!ModelState.IsValid)
        {
            return View("Frete", new ConfiguracaoFreteLojaViewModel
            {
                LojaId = loja.Id,
                LojaNome = loja.Nome,
                LojaSlug = loja.Slug,
                Faixas = existentes.OrderBy(item => item.Ordem).ToArray(),
                NovaFaixa = model
            });
        }

        _context.DefinirLojaAtual(id);
        _context.FaixaFreteLoja.Add(nova);
        await _context.SaveChangesAsync();
        TempData["Sucesso"] = "Faixa de frete adicionada.";
        return RedirectToRoute("ConfigurarFreteLojaAdmin", new { id });
    }

    [HttpPost("/Admin/Loja/{id:int}/Frete/{faixaId:int}/Excluir")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExcluirFaixaFrete(int id, int faixaId)
    {
        _context.DefinirLojaAtual(id);
        var faixa = await _context.FaixaFreteLoja
            .SingleOrDefaultAsync(item => item.Id == faixaId && item.LojaId == id);
        if (faixa == null)
            return NotFound();

        _context.FaixaFreteLoja.Remove(faixa);
        await _context.SaveChangesAsync();
        TempData["Sucesso"] = "Faixa de frete removida.";
        return RedirectToRoute("ConfigurarFreteLojaAdmin", new { id });
    }

    private async Task ValidarUnicidadeAsync(LojaViewModel model)
    {
        if (await _context.Loja.AnyAsync(loja => loja.Id != model.Id && loja.Slug == model.Slug))
        {
            ModelState.AddModelError(nameof(model.Slug), "Este slug já está sendo utilizado.");
        }

        if (!string.IsNullOrWhiteSpace(model.Dominio) &&
            await _context.Loja.AnyAsync(loja => loja.Id != model.Id && loja.Dominio == model.Dominio))
        {
            ModelState.AddModelError(nameof(model.Dominio), "Este domínio já está sendo utilizado.");
        }
    }

    private static void Normalizar(LojaViewModel model)
    {
        model.Nome = (model.Nome ?? string.Empty).Trim();
        model.Slug = (model.Slug ?? string.Empty).Trim().ToLowerInvariant();
        model.Dominio = NormalizarDominio(model.Dominio);
        model.LogoUrl = string.IsNullOrWhiteSpace(model.LogoUrl) ? null : model.LogoUrl.Trim();
        model.CorPrimaria = (model.CorPrimaria ?? string.Empty).Trim();
        model.CorSecundaria = (model.CorSecundaria ?? string.Empty).Trim();
        model.Descricao = NormalizarOpcional(model.Descricao);
        model.EmailContato = NormalizarOpcional(model.EmailContato)?.ToLowerInvariant();
        model.InstagramUrl = NormalizarOpcional(model.InstagramUrl);
        model.Whatsapp = string.IsNullOrWhiteSpace(model.Whatsapp)
            ? null
            : new string(model.Whatsapp.Where(char.IsDigit).ToArray());
        model.CepOrigem = NormalizarOpcional(model.CepOrigem);
        model.EnderecoOrigem = NormalizarOpcional(model.EnderecoOrigem);
        model.NumeroOrigem = NormalizarOpcional(model.NumeroOrigem);
        model.ComplementoOrigem = NormalizarOpcional(model.ComplementoOrigem);
        model.BairroOrigem = NormalizarOpcional(model.BairroOrigem);
        model.MunicipioOrigem = NormalizarOpcional(model.MunicipioOrigem);
        model.UfOrigem = NormalizarOpcional(model.UfOrigem)?.ToUpperInvariant();
        if (!model.AssinaturaInicioEm.HasValue && !model.AssinaturaTerminoEm.HasValue)
        {
            model.AssinaturaInicioEm = DateTime.Today;
            model.AssinaturaTerminoEm = DateTime.Today.AddYears(1);
        }
    }

    private void ValidarConfiguracaoEntrega(LojaViewModel model)
    {
        if (!model.RetiradaAtiva && !model.EntregaAtiva)
            ModelState.AddModelError(string.Empty, "Ative ao menos retirada ou entrega.");

        if (model.EntregaAtiva)
        {
            if (string.IsNullOrWhiteSpace(model.CepOrigem))
                ModelState.AddModelError(nameof(model.CepOrigem), "Informe o CEP de origem.");
            if (string.IsNullOrWhiteSpace(model.EnderecoOrigem))
                ModelState.AddModelError(nameof(model.EnderecoOrigem), "Informe o endereço de origem.");
            if (string.IsNullOrWhiteSpace(model.NumeroOrigem))
                ModelState.AddModelError(nameof(model.NumeroOrigem), "Informe o número de origem.");
            if (string.IsNullOrWhiteSpace(model.MunicipioOrigem))
                ModelState.AddModelError(nameof(model.MunicipioOrigem), "Informe a cidade de origem.");
            if (string.IsNullOrWhiteSpace(model.UfOrigem))
                ModelState.AddModelError(nameof(model.UfOrigem), "Informe a UF de origem.");
        }

        if (model.AssinaturaInicioEm.HasValue != model.AssinaturaTerminoEm.HasValue)
            ModelState.AddModelError(string.Empty, "Informe início e término da assinatura.");
        if (model.AssinaturaInicioEm.HasValue && model.AssinaturaTerminoEm.HasValue
            && model.AssinaturaTerminoEm.Value.Date != model.AssinaturaInicioEm.Value.Date.AddYears(1))
            ModelState.AddModelError(nameof(model.AssinaturaTerminoEm), "A assinatura deve possuir vigência de 12 meses.");
    }

    private static string? NormalizarDominio(string? dominio)
    {
        if (string.IsNullOrWhiteSpace(dominio))
        {
            return null;
        }

        var valor = dominio.Trim();
        if (Uri.TryCreate(valor, UriKind.Absolute, out var uri))
        {
            return uri.Host.ToLowerInvariant();
        }

        return valor.Split('/')[0].Split(':')[0].ToLowerInvariant();
    }

    private static string? NormalizarOpcional(string? valor)
    {
        return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }

    private static void Aplicar(LojaViewModel model, LojaModel loja)
    {
        loja.Nome = model.Nome;
        loja.Slug = model.Slug;
        loja.Dominio = model.Dominio;
        loja.LogoUrl = model.LogoUrl;
        loja.CorPrimaria = model.CorPrimaria;
        loja.CorSecundaria = model.CorSecundaria;
        loja.Descricao = model.Descricao;
        loja.Whatsapp = model.Whatsapp;
        loja.EmailContato = model.EmailContato;
        loja.InstagramUrl = model.InstagramUrl;
        loja.Ativa = model.Ativa;
        loja.RetiradaAtiva = model.RetiradaAtiva;
        loja.EntregaAtiva = model.EntregaAtiva;
        loja.CepOrigem = model.CepOrigem;
        loja.EnderecoOrigem = model.EnderecoOrigem;
        loja.NumeroOrigem = model.NumeroOrigem;
        loja.ComplementoOrigem = model.ComplementoOrigem;
        loja.BairroOrigem = model.BairroOrigem;
        loja.MunicipioOrigem = model.MunicipioOrigem;
        loja.UfOrigem = model.UfOrigem;
        loja.PercentualConsumoEntrega = model.PercentualConsumoEntrega;
        loja.AssinaturaInicioEm = model.AssinaturaInicioEm?.Date;
        loja.AssinaturaTerminoEm = model.AssinaturaTerminoEm?.Date;
    }

    private static LojaViewModel ParaViewModel(LojaModel loja)
    {
        return new LojaViewModel
        {
            Id = loja.Id,
            Nome = loja.Nome,
            Slug = loja.Slug,
            Dominio = loja.Dominio,
            LogoUrl = loja.LogoUrl,
            CorPrimaria = loja.CorPrimaria ?? "#0d6efd",
            CorSecundaria = loja.CorSecundaria ?? "#ffffff",
            Descricao = loja.Descricao,
            Whatsapp = loja.Whatsapp,
            EmailContato = loja.EmailContato,
            InstagramUrl = loja.InstagramUrl,
            RetiradaAtiva = loja.RetiradaAtiva,
            EntregaAtiva = loja.EntregaAtiva,
            CepOrigem = loja.CepOrigem,
            EnderecoOrigem = loja.EnderecoOrigem,
            NumeroOrigem = loja.NumeroOrigem,
            ComplementoOrigem = loja.ComplementoOrigem,
            BairroOrigem = loja.BairroOrigem,
            MunicipioOrigem = loja.MunicipioOrigem,
            UfOrigem = loja.UfOrigem,
            PercentualConsumoEntrega = loja.PercentualConsumoEntrega,
            AssinaturaInicioEm = loja.AssinaturaInicioEm,
            AssinaturaTerminoEm = loja.AssinaturaTerminoEm,
            Ativa = loja.Ativa
        };
    }
}
