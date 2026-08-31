using AutoMapper;
using BliviPedidos.Data;
using BliviPedidos.Models;
using BliviPedidos.Models.ViewModels;
using BliviPedidos.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BliviPedidos.Services.Implementations
{
    public class ProdutoService : BaseService<ItemPedido>, IProdutoService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ProdutoService> _logger;

        public ProdutoService(
            ApplicationDbContext context,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            ILogger<ProdutoService> logger) : base(context)
        {
            _context = context;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<List<Produto>> GetProdutosAsync()
        {
            return await _context.Produto.ToListAsync();
        }

        public async Task<List<Produto>> GetProdutosAtivosAsync()
        {
            return await _context.Produto
                                 .Where(p => p.IsAtivo)  // Apenas produtos ativos
                                 .ToListAsync();
        }

        public async Task<List<Produto>> GetProdutosDesativadosAsync()
        {
            return await _context.Produto
                                 .Where(p => !p.IsAtivo)  // Apenas produtos desativados
                                 .ToListAsync();
        }

        public async Task<ProdutoViewModel> ProcurarProdutoAsync(int id)
        {
            var produto = await _context.Produto
                .Include(p => p.ProdutoMovimentacao)
                .Include(p => p.Categoria) // Inclui a categoria relacionada
                .FirstOrDefaultAsync(p => p.Id == id);

            if (produto == null)
                return null;

            return _mapper.Map<ProdutoViewModel>(produto);
        }


        public async Task RegistrarCancelamentoProdutos(ItemPedido itemPedido)
        {
            var usuario = _httpContextAccessor.HttpContext?.User.Identity?.Name ?? "SISTEMA";

            var produto = await _context.Produto
                .Include(p => p.ProdutoMovimentacao)
                .FirstOrDefaultAsync(p => p.Id == itemPedido.Produto.Id);

            if (produto != null)
            {
                produto.Quantidade += itemPedido.Quantidade;

                var movimentacao = new ProdutoMovimentacao
                {
                    ProdutoId = produto.Id,
                    Quantidade = itemPedido.Quantidade,
                    Tipo = "Entrada",
                    LojaId = produto.LojaId,
                    PedidoId = itemPedido.PedidoId,
                    Ator = usuario,
                    Origem = OrigemMovimentacaoEstoque.CancelamentoPedido,
                    Observacao = $"[ENTRADA] | [PEDIDO-CANCELAMENTO ] | [{usuario.ToUpper()}]",
                    Data = DateTime.Now
                };

                await RegistrarMovimentacaoAsync(movimentacao);

                _context.Produto.Update(produto);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> RegistrarProdutoAsync(Produto produto)
        {
            var usuario = _httpContextAccessor.HttpContext?.User.Identity?.Name ?? "SISTEMA";

            try
            {
                if (produto.Id > 0)
                {

                    var produtoAtual = await _context.Produto
                        .Include(p => p.ProdutoMovimentacao)
                        .FirstOrDefaultAsync(p => p.Id == produto.Id);

                    if (produtoAtual != null)
                    {
                        // Verificação separada para movimentação de estoque
                        if (produtoAtual.Quantidade != produto.Quantidade)
                        {
                            string tipoMovimentacao = produto.Quantidade > produtoAtual.Quantidade ? "Entrada" : "Saida";
                            var quantidadeMovimentada = Math.Abs(produto.Quantidade - produtoAtual.Quantidade);

                            var movimentacao = new ProdutoMovimentacao
                            {
                                ProdutoId = produto.Id,
                                Quantidade = quantidadeMovimentada,
                                Tipo = tipoMovimentacao,
                                LojaId = produtoAtual.LojaId,
                                Ator = usuario,
                                Origem = OrigemMovimentacaoEstoque.EdicaoEstoque,
                                Observacao = $"[{tipoMovimentacao.ToUpper()}] | [CADASTRO-EDITAR] | [{usuario.ToUpper()}]",
                                Data = DateTime.Now
                            };

                            var movimentacaoId = await RegistrarMovimentacaoAsync(movimentacao);

                            // Adiciona a movimentação ao produto atual
                            produtoAtual.ProdutoMovimentacao.Add(movimentacao);

                            // Atualiza a quantidade do produto
                            produtoAtual.Quantidade = produto.Quantidade;
                        }

                        produtoAtual.PrecoPago = produto.PrecoPago;
                        produtoAtual.PrecoVenda = produto.PrecoVenda;

                        // Atualiza o estado de atividade do produto
                        produtoAtual.IsAtivo = produto.IsAtivo;
                        produtoAtual.CategoriaId = produto.CategoriaId;
                        produtoAtual.Foto = string.IsNullOrWhiteSpace(produto.Foto)
                            ? null
                            : produto.Foto.Trim();

                        // Salva as mudanças no banco de dados
                        _context.Produto.Update(produtoAtual);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    produto.Foto = string.IsNullOrWhiteSpace(produto.Foto)
                        ? null
                        : produto.Foto.Trim();

                    _context.Produto.Add(produto);
                    await _context.SaveChangesAsync();

                    // Criar uma movimentação de entrada para novo produto
                    var movimentacao = new ProdutoMovimentacao
                    {
                        ProdutoId = produto.Id,
                        Quantidade = produto.Quantidade,
                        Tipo = "Entrada",
                        LojaId = produto.LojaId,
                        Ator = usuario,
                        Origem = OrigemMovimentacaoEstoque.CadastroProduto,
                        Observacao = $"[ENTRADA] | [CADASTRO-NOVO] [{usuario.ToUpper()}]",
                        Data = DateTime.Now
                    };

                    // Adicionando a movimentação ao contexto
                    var movimentacaoId = await RegistrarMovimentacaoAsync(movimentacao);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Falha ao registrar produto. ProdutoId: {ProdutoId}",
                    produto.Id);
                return false;
            }
        }

        public bool UpdateQuantidade(List<ItemPedido> itens)
        {
            var usuario = _httpContextAccessor.HttpContext?.User.Identity?.Name ?? "SISTEMA";
            var pedidoId = itens.FirstOrDefault()?.PedidoId;

            try
            {
                foreach (var item in itens)
                {
                    Produto produto = _context.Produto.FirstOrDefault(p => p.Id == item.Produto.Id);

                    if (produto != null)
                    {
                        if (item.Quantidade > produto.Quantidade)
                        {
                            throw new InvalidOperationException($"A quantidade de '{produto.Nome}' disponível em estoque é insuficiente.");
                        }

                        // Deduz a quantidade do estoque
                        produto.Quantidade -= item.Quantidade;

                        // Registrar movimentação de saída
                        var movimentacaoSaida = new ProdutoMovimentacao
                        {
                            ProdutoId = produto.Id,
                            Quantidade = item.Quantidade,
                            Tipo = "Saída",
                            LojaId = produto.LojaId,
                            PedidoId = item.PedidoId,
                            Ator = usuario,
                            Origem = OrigemMovimentacaoEstoque.PedidoInterno,
                            Observacao = $"[SAIDA] | [PEDIDO-REALIZADO] | {usuario.ToUpper()} | PEDIDO: [{item.Pedido.Id.ToString()}]",
                            Data = DateTime.Now
                        };

                        // Registrar a movimentação
                        RegistrarMovimentacaoAsync(movimentacaoSaida).Wait();

                        _context.SaveChanges();
                    }
                    else
                    {
                        throw new InvalidOperationException("Produto não encontrado.");
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Falha ao baixar estoque do pedido. PedidoId: {PedidoId}, TotalItens: {TotalItens}",
                    pedidoId,
                    itens.Count);
                return false;
            }
        }

        public async Task<int> RegistrarMovimentacaoAsync(ProdutoMovimentacao movimentacao)
        {
            try
            {
                _context.ProdutoMovimentacao.Add(movimentacao);
                await _context.SaveChangesAsync();

                return movimentacao.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Falha ao registrar movimentacao de estoque. ProdutoId: {ProdutoId}, Tipo: {TipoMovimentacao}, Quantidade: {Quantidade}",
                    movimentacao.ProdutoId,
                    movimentacao.Tipo,
                    movimentacao.Quantidade);
                throw;
            }
        }

        public Task<bool> VerificarExistenciaProdutoNoBanco(string nome, decimal precoPago)
        {
            return _context.Produto.AnyAsync(p => p.Nome.Trim().ToUpper() == nome.Trim().ToUpper() && p.PrecoPago == precoPago);
        }

        public bool VerificarProdutoVinculadoPedido(int produtoId)
        {
            return dbSet.Any(item => item.Produto.Id == produtoId);
        }

        public List<ProdutoMovimentacao> GetMovimentacaoEstoque()
        {
            return _context.ProdutoMovimentacao
            .Include(m => m.Produto)
            .OrderByDescending(m => m.Data) // Ordena pela data mais recente
            .ToList();

        }

        public async Task<IEnumerable<Produto>> GetProdutosEmEstoqueByQuantidadeAsync(int quantidade)
        {
            return await _context.Produto
                                 .Include(p => p.ProdutoMovimentacao) // Inclui as movimentações associadas
                                 .Where(p => p.Quantidade <= quantidade)
                                 .ToListAsync();
        }
    }
}
