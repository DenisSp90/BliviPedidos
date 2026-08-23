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
