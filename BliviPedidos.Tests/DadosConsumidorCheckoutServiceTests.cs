using BliviPedidos.Models;
using BliviPedidos.Services.Implementations;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace BliviPedidos.Tests;

public class DadosConsumidorCheckoutServiceTests
{
    [Fact]
    public void DadosDevemSerNormalizadosEMantidosSeparadosPorLoja()
    {
        var contexto = new DefaultHttpContext { Session = new SessaoTeste() };
        var service = new DadosConsumidorCheckoutService(
            new HttpContextAccessor { HttpContext = contexto });

        service.Salvar(1, new DadosConsumidorCheckout
        {
            Nome = "  Maria Silva  ",
            Telefone = " 11 99999-9999 ",
            Email = " MARIA@EXEMPLO.COM ",
            UF = "sp"
        });

        var dados = Assert.IsType<DadosConsumidorCheckout>(service.Obter(1));
        Assert.Equal("Maria Silva", dados.Nome);
        Assert.Equal("maria@exemplo.com", dados.Email);
        Assert.Equal("SP", dados.UF);
        Assert.Null(service.Obter(2));
        service.Limpar(1);
        Assert.Null(service.Obter(1));
    }

    private sealed class SessaoTeste : ISession
    {
        private readonly Dictionary<string, byte[]> _dados = [];
        public IEnumerable<string> Keys => _dados.Keys;
        public string Id { get; } = Guid.NewGuid().ToString("N");
        public bool IsAvailable => true;
        public void Clear() => _dados.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _dados.Remove(key);
        public void Set(string key, byte[] value) => _dados[key] = value;
        public bool TryGetValue(string key, out byte[] value) => _dados.TryGetValue(key, out value!);
    }
}
