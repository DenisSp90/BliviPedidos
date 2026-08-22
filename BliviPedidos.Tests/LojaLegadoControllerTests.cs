using BliviPedidos.Controllers;
using BliviPedidos.Seguranca;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace BliviPedidos.Tests;

public class LojaLegadoControllerTests
{
    [Theory]
    [InlineData(null, "", "/Admin/Loja")]
    [InlineData("Index", "", "/Admin/Loja/Index")]
    [InlineData("Editar/7", "?origem=favorito", "/Admin/Loja/Editar/7?origem=favorito")]
    public void Redirecionar_DevePreservarCaminhoQueryEMetodo(
        string? caminho,
        string query,
        string destinoEsperado)
    {
        var controller = new LojaLegadoController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        controller.Request.QueryString = new QueryString(query);

        var resposta = caminho switch
        {
            null => controller.Raiz(),
            "Index" => controller.Index(),
            "Editar/7" => controller.Editar(7),
            _ => throw new InvalidOperationException("Cenário de teste não configurado.")
        };
        var result = Assert.IsType<RedirectResult>(resposta);

        Assert.Equal(destinoEsperado, result.Url);
        Assert.False(result.Permanent);
        Assert.True(result.PreserveMethod);
    }

    [Fact]
    public void ControllerLegado_DeveContinuarExigindoAdministracao()
    {
        var authorize = Assert.Single(typeof(LojaLegadoController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>());

        Assert.Equal(PoliticasAutorizacao.Administracao, authorize.Policy);
    }
}
