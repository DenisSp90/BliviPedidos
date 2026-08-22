using BliviPedidos.Data;
using BliviPedidos.Dtos.Publico;
using BliviPedidos.Models;
using BliviPedidos.Models.ViewModels;
using BliviPedidos.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BliviPedidos.Services.Implementations;

public sealed class CalculadorCarrinhoPublicoService : ICalculadorCarrinhoPublicoService
{
    private readonly ApplicationDbContext _context;

    public CalculadorCarrinhoPublicoService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ResultadoValidacaoCarrinhoPublico> ValidarERecalcularAsync(
        CarrinhoPublico carrinho,
        CancellationToken cancellationToken = default)
    {
        var erros = new List<string>();
        var itensInvalidos = carrinho.Itens.Where(item => item.ProdutoId <= 0 || item.Quantidade <= 0).ToArray();
        if (itensInvalidos.Length > 0)
        {
            erros.Add("O carrinho contém produto ou quantidade inválida.");
        }

        var grupos = carrinho.Itens
            .Where(item => item.ProdutoId > 0 && item.Quantidade > 0)
            .GroupBy(item => item.ProdutoId)
            .ToArray();
        if (grupos.Any(grupo => grupo.Count() > 1))
        {
            erros.Add("O carrinho contém produtos duplicados.");
        }

        var quantidades = grupos.ToDictionary(
            grupo => grupo.Key,
            grupo => grupo.Sum(item => (long)item.Quantidade));
        if (quantidades.Count == 0)
        {
            return new ResultadoValidacaoCarrinhoPublico { Erros = erros };
        }

        var produtosIds = quantidades.Keys.ToArray();
        var produtos = await _context.Produto
            .AsNoTracking()
            .Where(produto => produto.LojaId == carrinho.LojaId && produtosIds.Contains(produto.Id))
            .Select(produto => new
            {
                Produto = new ProdutoPublicoDto
                {
                    Id = produto.Id,
                    Codigo = produto.Codigo,
                    Nome = produto.Nome,
                    PrecoVenda = produto.PrecoVenda,
                    Tamanho = produto.Tamanho,
                    Foto = produto.Foto,
                    CategoriaId = produto.CategoriaId,
                    CategoriaNome = produto.Categoria != null ? produto.Categoria.Nome : null
                },
                produto.IsAtivo,
                produto.Quantidade
            })
            .ToListAsync(cancellationToken);

        var encontrados = produtos.Select(item => item.Produto.Id).ToHashSet();
        if (produtosIds.Any(id => !encontrados.Contains(id)))
        {
            erros.Add("Um ou mais produtos do carrinho não existem nesta loja.");
        }

        foreach (var produto in produtos)
        {
            var solicitada = quantidades[produto.Produto.Id];
            if (!produto.IsAtivo)
            {
                erros.Add($"O produto “{produto.Produto.Nome}” não está mais disponível.");
            }
            else if (solicitada > produto.Quantidade)
            {
                erros.Add($"A quantidade solicitada de “{produto.Produto.Nome}” não está disponível.");
            }
        }

        var itens = produtos
            .Where(produto => quantidades[produto.Produto.Id] <= int.MaxValue)
            .Select(produto => new ItemCarrinhoPublicoViewModel
            {
                Produto = produto.Produto,
                Quantidade = (int)quantidades[produto.Produto.Id]
            })
            .ToArray();

        return new ResultadoValidacaoCarrinhoPublico
        {
            Itens = itens,
            Erros = erros
        };
    }
}
