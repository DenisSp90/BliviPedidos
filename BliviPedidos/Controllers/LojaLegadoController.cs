using BliviPedidos.Seguranca;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BliviPedidos.Controllers;

[Authorize(Policy = PoliticasAutorizacao.Administracao)]
[Route("Loja/{**caminho}")]
public class LojaLegadoController : Controller
{
    [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE")]
    public IActionResult Redirecionar(string? caminho)
    {
        var destino = $"{Request.PathBase}/Admin/Loja";

        if (!string.IsNullOrWhiteSpace(caminho))
        {
            destino += $"/{caminho}";
        }

        destino += Request.QueryString.Value;

        return new RedirectResult(destino, permanent: false, preserveMethod: true);
    }
}
