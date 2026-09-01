using BliviPedidos.Data;
using BliviPedidos.Models;
using BliviPedidos.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BliviPedidos.Services.Implementations;

public class ItemPedidoService : BaseService<ItemPedido>, IItemPedidoService
{
    private readonly IProdutoService _produtoService;
    private readonly IHttpContextAccessor _httpContextAccessor;


    public ItemPedidoService(
        ApplicationDbContext context, 
        IProdutoService produtoService, 
        IHttpContextAccessor httpContextAccessor) : base(context)
    {
        _produtoService = produtoService;
        _httpContextAccessor = httpContextAccessor;
    }

    public List<ItemPedido> GetAllItensPedidos()
    {
        return dbSet.ToList();
    }

    public ItemPedido GetItemPedido(int itemPedidoId)
    {
        return
            dbSet
                .Where(ip => ip.Id == itemPedidoId)
                .SingleOrDefault();
    }

    public void RemoveItemPedido(int itemPedidoId)
    {
        dbSet.Remove(GetItemPedido(itemPedidoId));
    }

    public async Task UpdateItemPedidoAsync(int itemPedidoId, int novoProdutoId, int quantidade, decimal preco)
    {
        _ = preco; // O preço recebido da tela não é confiável; usamos sempre o cadastro do produto.

        if (quantidade <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantidade), "A quantidade deve ser maior que zero.");

        var usuario = _httpContextAccessor.HttpContext?.User.Identity?.Name ?? "SISTEMA";

        var itemPedido = await dbSet
            .AsNoTracking()
            .Include(ip => ip.Produto)
            .Include(ip => ip.Pedido)
            .SingleOrDefaultAsync(ip => ip.Id == itemPedidoId);

        if (itemPedido == null)
            throw new KeyNotFoundException("Item de pedido não encontrado.");

        if (itemPedido.Pedido.Status is StatusPedido.Carrinho
            or StatusPedido.Concluido
            or StatusPedido.Cancelado)
            throw new InvalidOperationException("Os itens deste pedido não podem mais ser alterados.");

        var novoProduto = await _context.Produto
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == novoProdutoId);

        if (novoProduto == null)
            throw new KeyNotFoundException("Novo produto não encontrado.");

        var produtoAtual = itemPedido.Produto;

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            if (produtoAtual.Id == novoProduto.Id)
            {
                var diferenca = quantidade - itemPedido.Quantidade;
                if (diferenca > 0)
                {
                    var atualizado = await _context.Produto
                        .Where(p => p.Id == novoProduto.Id && p.Quantidade >= diferenca)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(p => p.Quantidade, p => p.Quantidade - diferenca));

                    if (atualizado != 1)
                        throw new InvalidOperationException("A quantidade solicitada excede a quantidade disponível do produto.");
                }
                else if (diferenca < 0)
                {
                    await _context.Produto
                        .Where(p => p.Id == novoProduto.Id)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(p => p.Quantidade, p => p.Quantidade - diferenca));
                }
            }
            else
            {
                var novoProdutoAtualizado = await _context.Produto
                    .Where(p => p.Id == novoProduto.Id && p.Quantidade >= quantidade)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(p => p.Quantidade, p => p.Quantidade - quantidade));

                if (novoProdutoAtualizado != 1)
                    throw new InvalidOperationException("A quantidade solicitada excede a quantidade disponível do novo produto.");

                await _context.Produto
                    .Where(p => p.Id == produtoAtual.Id)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(p => p.Quantidade, p => p.Quantidade + itemPedido.Quantidade));
            }

        // Criar movimentação de entrada para o produto atual (devolução do estoque)
        var movimentacaoEntrada = new ProdutoMovimentacao
        {
            ProdutoId = produtoAtual.Id,
            LojaId = itemPedido.Pedido.LojaId,
            PedidoId = itemPedido.Pedido.Id,
            Quantidade = itemPedido.Quantidade,
            Tipo = "Entrada",
            Ator = usuario,
            Origem = OrigemMovimentacaoEstoque.AlteracaoPedido,
            Observacao = $"[ENTRADA] | [PEDIDO-ALTERAÇÃO] | {usuario.ToUpper()} | PEDIDO: [{itemPedido.Pedido.Id.ToString()}]",
            Data = DateTime.Now
        };

        // Criar movimentação de saída para o novo produto (retirada do estoque)
        var movimentacaoSaida = new ProdutoMovimentacao
        {
            ProdutoId = novoProduto.Id,
            LojaId = itemPedido.Pedido.LojaId,
            PedidoId = itemPedido.Pedido.Id,
            Quantidade = quantidade,
            Tipo = "Saída",
            Ator = usuario,
            Origem = OrigemMovimentacaoEstoque.AlteracaoPedido,
            Observacao = $"[SAIDA] | [PEDIDO-ALTERAÇÃO] | {usuario.ToUpper()} | PEDIDO: [{itemPedido.Pedido.Id.ToString()}]",
            Data = DateTime.Now
        };

            _context.ProdutoMovimentacao.AddRange(movimentacaoEntrada, movimentacaoSaida);

            var itemAtualizado = await dbSet
                .Where(ip => ip.Id == itemPedido.Id
                    && ip.Pedido.Status != StatusPedido.Carrinho
                    && ip.Pedido.Status != StatusPedido.Concluido
                    && ip.Pedido.Status != StatusPedido.Cancelado)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(ip => ip.ProdutoId, novoProduto.Id)
                    .SetProperty(ip => ip.Quantidade, quantidade)
                    .SetProperty(ip => ip.PrecoUnitario, novoProduto.PrecoVenda));

            if (itemAtualizado != 1)
                throw new InvalidOperationException("O pedido mudou de situação e seus itens não podem mais ser alterados.");

            var novoTotal = await dbSet
                .Where(ip => ip.PedidoId == itemPedido.PedidoId)
                .SumAsync(ip => ip.Quantidade * ip.PrecoUnitario);

            await _context.Pedido
                .Where(p => p.Id == itemPedido.PedidoId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.ValorTotalPedido, novoTotal));

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

}
