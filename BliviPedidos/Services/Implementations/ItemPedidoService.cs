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

        var usuario = _httpContextAccessor.HttpContext.User.Identity.Name;

        var itemPedido = await dbSet
            .Include(ip => ip.Produto) 
            .Include(ip => ip.Pedido)  
            .SingleOrDefaultAsync(ip => ip.Id == itemPedidoId);

        if (itemPedido == null)
            throw new KeyNotFoundException("Item de pedido não encontrado.");

        var novoProduto = await _context.Produto
            .SingleOrDefaultAsync(p => p.Id == novoProdutoId);

        if (novoProduto == null)
            throw new KeyNotFoundException("Novo produto não encontrado.");

        if (quantidade > novoProduto.Quantidade)
            throw new InvalidOperationException("A quantidade solicitada excede a quantidade disponível do novo produto.");

        var produtoAtual = itemPedido.Produto;
        produtoAtual.Quantidade += itemPedido.Quantidade; // Reponha a quantidade do produto atual
        novoProduto.Quantidade -= quantidade; // Deduz a quantidade do novo produto

        // Criar movimentação de entrada para o produto atual (devolução do estoque)
        var movimentacaoEntrada = new ProdutoMovimentacao
        {
            ProdutoId = produtoAtual.Id,
            Quantidade = itemPedido.Quantidade,
            Tipo = "Entrada",
            Observacao = $"[ENTRADA] | [PEDIDO-ALTERAÇÃO] | {usuario.ToUpper()} | PEDIDO: [{itemPedido.Pedido.Id.ToString()}]",
            Data = DateTime.Now
        };

        // Criar movimentação de saída para o novo produto (retirada do estoque)
        var movimentacaoSaida = new ProdutoMovimentacao
        {
            ProdutoId = novoProduto.Id,
            Quantidade = quantidade,
            Tipo = "Saída",
            Observacao = $"[SAIDA] | [PEDIDO-ALTERAÇÃO] | {usuario.ToUpper()} | PEDIDO: [{itemPedido.Pedido.Id.ToString()}]",
            Data = DateTime.Now
        };

        // Registrar as movimentações
        await _produtoService.RegistrarMovimentacaoAsync(movimentacaoEntrada);
        await _produtoService.RegistrarMovimentacaoAsync(movimentacaoSaida);

        // Atualize as propriedades do item de pedido
        itemPedido.Produto = novoProduto; // Atualize a referência para o novo produto
        itemPedido.Quantidade = quantidade;
        itemPedido.PrecoUnitario = novoProduto.PrecoVenda;

        dbSet.Update(itemPedido);

        // Atualize os produtos no banco de dados
        _context.Produto.Update(produtoAtual);
        _context.Produto.Update(novoProduto);

        // Salve as mudanças no banco de dados
        await _context.SaveChangesAsync();
    }

}
