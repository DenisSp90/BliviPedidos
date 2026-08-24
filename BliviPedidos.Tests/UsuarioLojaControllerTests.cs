using BliviPedidos.Controllers;
using BliviPedidos.Seguranca;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace BliviPedidos.Tests;

public sealed class UsuarioLojaControllerTests
{
    [Fact]
    public void Controller_DeveExigirPermissaoDeVendas()
    {
        var authorize = Assert.Single(typeof(UsuarioLojaController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>());

        Assert.Equal(PoliticasAutorizacao.Vendas, authorize.Policy);
    }
}
