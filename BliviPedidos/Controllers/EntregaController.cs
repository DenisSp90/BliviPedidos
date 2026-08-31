using BliviPedidos.Data;
using BliviPedidos.Models;
using BliviPedidos.Models.ViewModels;
using BliviPedidos.Seguranca;
using BliviPedidos.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BliviPedidos.Controllers;

[Authorize(Policy = PoliticasAutorizacao.Vendas)]
public sealed class EntregaController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ICalculadorFreteLojaService _calculador;

    public EntregaController(ApplicationDbContext context, ICalculadorFreteLojaService calculador)
    {
        _context = context;
        _calculador = calculador;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var loja = await ObterLojaAsync();
        return loja == null ? NotFound() : View(await CriarViewModelAsync(loja));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Salvar(EntregaLojaViewModel model)
    {
        var loja = await ObterLojaAsync();
        if (loja == null)
            return NotFound();

        Normalizar(model);
        var faixas = await ObterFaixasAsync();
        Validar(model, faixas);
        if (!ModelState.IsValid)
            return View("Index", await CriarViewModelAsync(loja, model));

        loja.RetiradaAtiva = model.RetiradaAtiva;
        loja.EntregaAtiva = model.EntregaAtiva;
        loja.CepOrigem = model.CepOrigem;
        loja.EnderecoOrigem = model.EnderecoOrigem;
        loja.NumeroOrigem = model.NumeroOrigem;
        loja.ComplementoOrigem = model.ComplementoOrigem;
        loja.BairroOrigem = model.BairroOrigem;
        loja.MunicipioOrigem = model.MunicipioOrigem;
        loja.UfOrigem = model.UfOrigem;
        await _context.SaveChangesAsync();
        TempData["Sucesso"] = "Configuração de atendimento atualizada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdicionarFaixa(FaixaFreteViewModel model)
    {
        var loja = await ObterLojaAsync();
        if (loja == null)
            return NotFound();

        var existentes = await ObterFaixasAsync();
        var faixa = new FaixaFreteLoja
        {
            DistanciaInicialKm = model.DistanciaInicialKm,
            DistanciaFinalKm = model.DistanciaFinalKm,
            ValorFrete = model.ValorFrete,
            Ativa = true,
            Ordem = existentes.Select(item => item.Ordem).DefaultIfEmpty(0).Max() + 1
        };
        foreach (var erro in _calculador.Validar(existentes.Append(faixa)))
            ModelState.AddModelError(string.Empty, erro);

        if (!ModelState.IsValid)
        {
            var vm = await CriarViewModelAsync(loja);
            return View("Index", new EntregaLojaViewModel
            {
                LojaNome = vm.LojaNome,
                RetiradaAtiva = vm.RetiradaAtiva,
                EntregaAtiva = vm.EntregaAtiva,
                CepOrigem = vm.CepOrigem,
                EnderecoOrigem = vm.EnderecoOrigem,
                NumeroOrigem = vm.NumeroOrigem,
                ComplementoOrigem = vm.ComplementoOrigem,
                BairroOrigem = vm.BairroOrigem,
                MunicipioOrigem = vm.MunicipioOrigem,
                UfOrigem = vm.UfOrigem,
                PercentualConsumoEntrega = vm.PercentualConsumoEntrega,
                AssinaturaInicioEm = vm.AssinaturaInicioEm,
                AssinaturaTerminoEm = vm.AssinaturaTerminoEm,
                Faixas = vm.Faixas,
                NovaFaixa = model
            });
        }

        _context.FaixaFreteLoja.Add(faixa);
        await _context.SaveChangesAsync();
        TempData["Sucesso"] = "Faixa de frete adicionada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExcluirFaixa(int id)
    {
        var faixa = await _context.FaixaFreteLoja.SingleOrDefaultAsync(item => item.Id == id);
        if (faixa == null)
            return NotFound();

        var restantes = (await ObterFaixasAsync()).Where(item => item.Id != id).ToArray();
        var erros = _calculador.Validar(restantes);
        if (erros.Count > 0)
        {
            TempData["Erro"] = string.Join(" ", erros);
            return RedirectToAction(nameof(Index));
        }

        _context.FaixaFreteLoja.Remove(faixa);
        var loja = await ObterLojaAsync();
        if (loja != null && restantes.All(item => !item.Ativa))
            loja.EntregaAtiva = false;
        await _context.SaveChangesAsync();
        TempData["Sucesso"] = "Faixa de frete removida.";
        return RedirectToAction(nameof(Index));
    }

    private Task<Loja?> ObterLojaAsync() => _context.Loja
        .SingleOrDefaultAsync(item => item.Id == _context.LojaIdAtual);

    private async Task<List<FaixaFreteLoja>> ObterFaixasAsync() => await _context.FaixaFreteLoja
        .AsNoTracking()
        .OrderBy(item => item.Ordem)
        .ThenBy(item => item.DistanciaInicialKm)
        .ToListAsync();

    private async Task<EntregaLojaViewModel> CriarViewModelAsync(Loja loja, EntregaLojaViewModel? entrada = null)
    {
        var faixas = await ObterFaixasAsync();
        return new EntregaLojaViewModel
        {
            LojaNome = loja.Nome,
            RetiradaAtiva = entrada?.RetiradaAtiva ?? loja.RetiradaAtiva,
            EntregaAtiva = entrada?.EntregaAtiva ?? loja.EntregaAtiva,
            CepOrigem = entrada?.CepOrigem ?? loja.CepOrigem,
            EnderecoOrigem = entrada?.EnderecoOrigem ?? loja.EnderecoOrigem,
            NumeroOrigem = entrada?.NumeroOrigem ?? loja.NumeroOrigem,
            ComplementoOrigem = entrada?.ComplementoOrigem ?? loja.ComplementoOrigem,
            BairroOrigem = entrada?.BairroOrigem ?? loja.BairroOrigem,
            MunicipioOrigem = entrada?.MunicipioOrigem ?? loja.MunicipioOrigem,
            UfOrigem = entrada?.UfOrigem ?? loja.UfOrigem,
            PercentualConsumoEntrega = loja.PercentualConsumoEntrega,
            AssinaturaInicioEm = loja.AssinaturaInicioEm,
            AssinaturaTerminoEm = loja.AssinaturaTerminoEm,
            Faixas = faixas,
            NovaFaixa = new FaixaFreteViewModel
            {
                DistanciaInicialKm = faixas.Where(item => item.Ativa)
                    .Select(item => item.DistanciaFinalKm).DefaultIfEmpty(0m).Max()
            }
        };
    }

    private void Validar(EntregaLojaViewModel model, IReadOnlyCollection<FaixaFreteLoja> faixas)
    {
        if (!model.RetiradaAtiva && !model.EntregaAtiva)
            ModelState.AddModelError(string.Empty, "Ative ao menos retirada ou entrega.");
        if (!model.EntregaAtiva)
            return;
        if (string.IsNullOrWhiteSpace(model.CepOrigem)) ModelState.AddModelError(nameof(model.CepOrigem), "Informe o CEP.");
        if (string.IsNullOrWhiteSpace(model.EnderecoOrigem)) ModelState.AddModelError(nameof(model.EnderecoOrigem), "Informe o endereço.");
        if (string.IsNullOrWhiteSpace(model.NumeroOrigem)) ModelState.AddModelError(nameof(model.NumeroOrigem), "Informe o número.");
        if (string.IsNullOrWhiteSpace(model.MunicipioOrigem)) ModelState.AddModelError(nameof(model.MunicipioOrigem), "Informe a cidade.");
        if (string.IsNullOrWhiteSpace(model.UfOrigem)) ModelState.AddModelError(nameof(model.UfOrigem), "Informe a UF.");
        if (!faixas.Any(item => item.Ativa)) ModelState.AddModelError(nameof(model.EntregaAtiva), "Cadastre ao menos uma faixa ativa.");
    }

    private static void Normalizar(EntregaLojaViewModel model)
    {
        model.CepOrigem = Normalizar(model.CepOrigem);
        model.EnderecoOrigem = Normalizar(model.EnderecoOrigem);
        model.NumeroOrigem = Normalizar(model.NumeroOrigem);
        model.ComplementoOrigem = Normalizar(model.ComplementoOrigem);
        model.BairroOrigem = Normalizar(model.BairroOrigem);
        model.MunicipioOrigem = Normalizar(model.MunicipioOrigem);
        model.UfOrigem = Normalizar(model.UfOrigem)?.ToUpperInvariant();
    }

    private static string? Normalizar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
