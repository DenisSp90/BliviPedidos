using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BliviPedidos.Data;
using BliviPedidos.Areas.Loja.Controllers;
using BliviPedidos.Models;
using BliviPedidos.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using BliviPedidos.Services.Interfaces;
using Xunit;

namespace BliviPedidos.Tests;

public class ContaConsumidorTests
{
    [Fact]
    public void CadastroConsumidor_DeveExigirNomeCelularEmailESenha()
    {
        var model = new CriarContaConsumidorViewModel();
        var resultados = new List<ValidationResult>();

        var valido = Validator.TryValidateObject(model, new ValidationContext(model), resultados, true);

        Assert.False(valido);
        Assert.Contains(resultados, erro => erro.MemberNames.Contains(nameof(model.Nome)));
        Assert.Contains(resultados, erro => erro.MemberNames.Contains(nameof(model.Celular)));
        Assert.Contains(resultados, erro => erro.MemberNames.Contains(nameof(model.Email)));
        Assert.Contains(resultados, erro => erro.MemberNames.Contains(nameof(model.Senha)));
    }

    [Fact]
    public void LojaDoFormularioNaoDeveParticiparDaValidacaoDoPost()
    {
        var propriedade = typeof(CriarContaConsumidorViewModel)
            .GetProperty(nameof(CriarContaConsumidorViewModel.Loja));

        Assert.NotNull(propriedade);
        Assert.Single(propriedade!.GetCustomAttributes(typeof(ValidateNeverAttribute), true));
    }

    [Fact]
    public void DadosCheckout_DevemExigirNomeCelularEEmail()
    {
        var dados = new DadosConsumidorCheckout();
        var resultados = new List<ValidationResult>();

        var valido = Validator.TryValidateObject(dados, new ValidationContext(dados), resultados, true);

        Assert.False(valido);
        Assert.Contains(resultados, erro => erro.MemberNames.Contains(nameof(dados.Nome)));
        Assert.Contains(resultados, erro => erro.MemberNames.Contains(nameof(dados.Telefone)));
        Assert.Contains(resultados, erro => erro.MemberNames.Contains(nameof(dados.Email)));
    }

    [Fact]
    public void ConfirmacaoCheckout_DeveExigirAutenticacao()
    {
        var metodo = typeof(HomeController).GetMethod(nameof(HomeController.ConfirmarCheckout));
        Assert.Single(metodo!.GetCustomAttributes(typeof(AuthorizeAttribute), true));
    }

    [Fact]
    public async Task DetalhePedido_DeveRetornarNotFoundParaPedidoDeOutroConsumidor()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        context.Pedido.Add(new Pedido
        {
            CodigoPublico = "CODIGO-OUTRO-USUARIO",
            ConsumidorUsuarioId = null,
            Cadastro = new Cadastro { Nome = "Outro", Telefone = "11 99999-9999" }
        });
        await context.SaveChangesAsync();

        var lojaService = new Mock<ILojaAtualService>();
        lojaService.Setup(service => service.ObterLojaAsync()).ReturnsAsync(new Loja
        {
            Id = Loja.PadraoId,
            Nome = "Loja",
            Slug = "loja",
            Ativa = true
        });
        var controller = new ContaController(lojaService.Object, null!, null!, context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, "consumidor-atual")],
                        "Teste"))
                }
            }
        };

        var resultado = await controller.Pedido("loja", "CODIGO-OUTRO-USUARIO");

        Assert.IsType<NotFoundResult>(resultado);
    }
}
