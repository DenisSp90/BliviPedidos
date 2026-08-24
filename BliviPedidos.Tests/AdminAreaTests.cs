using BliviPedidos.Areas.Admin.Controllers;
using BliviPedidos.Services.Implementations;
using BliviPedidos.Seguranca;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace BliviPedidos.Tests;

public class AdminAreaTests
{
    [Fact]
    public void UsuarioController_DevePertencerAAreaAdminEExigirAdministrador()
    {
        var controllerType = typeof(UsuarioController);
        var area = Assert.Single(controllerType
            .GetCustomAttributes(typeof(AreaAttribute), true)
            .Cast<AreaAttribute>());
        var authorize = Assert.Single(controllerType
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>());

        Assert.Equal("Admin", area.RouteValue);
        Assert.Equal(PoliticasAutorizacao.Administracao, authorize.Policy);
    }

    [Theory]
    [InlineData("55 11 99999-9999", "Blivi@5511999999999")]
    [InlineData("+55 (21) 98888-7777", "Blivi@5521988887777")]
    public void SenhaPadrao_DeveUsarSomenteDigitosDoCelular(string celular, string senhaEsperada)
    {
        Assert.Equal(senhaEsperada, UsuarioController.CriarSenhaPadrao(celular));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("123")]
    public void SenhaPadrao_DeveRejeitarCelularInvalido(string? celular)
    {
        Assert.Null(UsuarioController.CriarSenhaPadrao(celular));
    }

    [Fact]
    public void HomeController_DevePertencerAAreaAdminEExigirAdministrador()
    {
        var controllerType = typeof(HomeController);

        var area = Assert.Single(controllerType.GetCustomAttributes(typeof(AreaAttribute), true).Cast<AreaAttribute>());
        var authorize = Assert.Single(controllerType.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());

        Assert.Equal("Admin", area.RouteValue);
        Assert.Equal(PoliticasAutorizacao.Administracao, authorize.Policy);
    }

    [Fact]
    public void Index_DeveExibirDashboardAdministrativo()
    {
        var controller = new HomeController();

        var result = controller.Index();

        Assert.IsType<ViewResult>(result);
    }
}
