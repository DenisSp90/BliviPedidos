using System.ComponentModel.DataAnnotations;
using BliviPedidos.Areas.Identity.Pages.Account;
using BliviPedidos.Services.Implementations;
using Xunit;

namespace BliviPedidos.Tests;

public class CadastroUsuarioTests
{
    [Fact]
    public void PerfilAusente_DeveSerInvalido()
    {
        var input = CriarInput(perfil: null);

        var resultados = Validar(input);

        Assert.Contains(resultados, resultado => resultado.MemberNames.Contains(nameof(input.Perfil)));
    }

    [Theory]
    [InlineData(InicializadorSistema.PerfilAdministrador)]
    [InlineData(InicializadorSistema.PerfilVendedor)]
    [InlineData(InicializadorSistema.PerfilEstoquista)]
    public void PerfilDoSistema_DeveSerValido(string perfil)
    {
        var input = CriarInput(perfil);

        var resultados = Validar(input);

        Assert.DoesNotContain(resultados, resultado => resultado.MemberNames.Contains(nameof(input.Perfil)));
    }

    private static RegisterModel.InputModel CriarInput(string? perfil)
    {
        return new RegisterModel.InputModel
        {
            Email = "usuario@teste.com",
            Password = "SenhaForte!123",
            ConfirmPassword = "SenhaForte!123",
            LojaId = 1,
            Perfil = perfil!
        };
    }

    private static List<ValidationResult> Validar(RegisterModel.InputModel input)
    {
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(input, new ValidationContext(input), resultados, validateAllProperties: true);
        return resultados;
    }
}
