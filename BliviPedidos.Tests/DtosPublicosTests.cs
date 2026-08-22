using BliviPedidos.Dtos.Publico;
using Xunit;

namespace BliviPedidos.Tests;

public class DtosPublicosTests
{
    [Theory]
    [InlineData("PrecoPago")]
    [InlineData("Quantidade")]
    [InlineData("CodeBar")]
    [InlineData("ProdutoMovimentacao")]
    [InlineData("LojaId")]
    [InlineData("Loja")]
    [InlineData("IsAtivo")]
    public void ProdutoPublico_NaoDeveExporCampoInterno(string propriedade)
    {
        Assert.Null(typeof(ProdutoPublicoDto).GetProperty(propriedade));
    }

    [Fact]
    public void LojaPublica_NaoDeveExporIdDominioOuStatusInterno()
    {
        var propriedades = typeof(LojaPublicaDto).GetProperties().Select(item => item.Name).ToArray();

        Assert.DoesNotContain("Id", propriedades);
        Assert.DoesNotContain("Dominio", propriedades);
        Assert.DoesNotContain("Ativa", propriedades);
        Assert.DoesNotContain("Usuarios", propriedades);
    }

    [Fact]
    public void CategoriaPublica_NaoDeveExporLojaStatusOuEntidades()
    {
        var propriedades = typeof(CategoriaPublicaDto).GetProperties().Select(item => item.Name).ToArray();

        Assert.Equal(["Id", "Nome"], propriedades);
    }
}
