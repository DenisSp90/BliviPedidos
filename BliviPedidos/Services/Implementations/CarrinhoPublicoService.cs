using System.Security.Cryptography;
using System.Text.Json;
using BliviPedidos.Models;
using BliviPedidos.Services.Interfaces;
using Microsoft.AspNetCore.WebUtilities;

namespace BliviPedidos.Services.Implementations;

public sealed class CarrinhoPublicoService : ICarrinhoPublicoService
{
    private const string PrefixoChave = "carrinho-publico:loja:";
    private const string ChaveIdentificador = "carrinho-publico:identificador";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CarrinhoPublicoService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public CarrinhoPublico Obter(int lojaId)
    {
        ValidarLoja(lojaId);
        var identificador = ObterIdentificador();
        var json = Sessao.GetString(ObterChave(identificador, lojaId));
        if (string.IsNullOrWhiteSpace(json))
        {
            return NovoCarrinho(identificador, lojaId);
        }

        var carrinho = JsonSerializer.Deserialize<CarrinhoPublico>(json, JsonOptions);
        return carrinho?.LojaId == lojaId && carrinho.Identificador == identificador
            ? carrinho
            : NovoCarrinho(identificador, lojaId);
    }

    public CarrinhoPublico Adicionar(int lojaId, int produtoId, int quantidade = 1)
    {
        ValidarLoja(lojaId);
        if (produtoId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(produtoId));
        }

        if (quantidade <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantidade));
        }

        var carrinho = Obter(lojaId);
        var item = carrinho.Itens.SingleOrDefault(item => item.ProdutoId == produtoId);
        if (item == null)
        {
            carrinho.Itens.Add(new ItemCarrinhoPublico
            {
                ProdutoId = produtoId,
                Quantidade = quantidade
            });
        }
        else
        {
            item.Quantidade += quantidade;
        }

        Salvar(carrinho);
        return carrinho;
    }

    public CarrinhoPublico Remover(int lojaId, int produtoId)
    {
        var carrinho = Obter(lojaId);
        carrinho.Itens.RemoveAll(item => item.ProdutoId == produtoId);
        Salvar(carrinho);
        return carrinho;
    }

    public void Limpar(int lojaId)
    {
        ValidarLoja(lojaId);
        Sessao.Remove(ObterChave(ObterIdentificador(), lojaId));
    }

    private ISession Sessao => _httpContextAccessor.HttpContext?.Session
        ?? throw new InvalidOperationException("Não existe uma sessão HTTP ativa para o carrinho público.");

    private void Salvar(CarrinhoPublico carrinho)
    {
        Sessao.SetString(
            ObterChave(carrinho.Identificador, carrinho.LojaId),
            JsonSerializer.Serialize(carrinho, JsonOptions));
    }

    private string ObterIdentificador()
    {
        var identificador = Sessao.GetString(ChaveIdentificador);
        if (!string.IsNullOrWhiteSpace(identificador))
        {
            return identificador;
        }

        identificador = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        Sessao.SetString(ChaveIdentificador, identificador);
        return identificador;
    }

    private static CarrinhoPublico NovoCarrinho(string identificador, int lojaId) => new()
    {
        Identificador = identificador,
        LojaId = lojaId
    };

    private static string ObterChave(string identificador, int lojaId) =>
        $"{PrefixoChave}{identificador}:loja:{lojaId}";

    private static void ValidarLoja(int lojaId)
    {
        if (lojaId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lojaId));
        }
    }
}
