using BliviPedidos.Seguranca;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BliviPedidos.Controllers;

[Authorize(Policy = PoliticasAutorizacao.Administracao)]
public class LojaLegadoController : Controller
{
    [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE")]
    [Route("Loja")]
    public IActionResult Raiz() => Redirecionar();

    [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE")]
    [Route("Loja/Index")]
    public IActionResult Index() => Redirecionar("Index");

    [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE")]
    [Route("Loja/Criar")]
    public IActionResult Criar() => Redirecionar("Criar");

    [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE")]
    [Route("Loja/Editar/{id?}")]
    public IActionResult Editar(int? id) => Redirecionar("Editar", id);

    [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE")]
    [Route("Loja/AlterarStatus/{id?}")]
    public IActionResult AlterarStatus(int? id) => Redirecionar("AlterarStatus", id);

    private IActionResult Redirecionar(string? acao = null, int? id = null)
    {
        var destino = $"{Request.PathBase}/Admin/Loja";

        if (!string.IsNullOrWhiteSpace(acao))
        {
            destino += $"/{acao}";
        }

        if (id.HasValue)
        {
            destino += $"/{id.Value}";
        }

        destino += Request.QueryString.Value;

        return new RedirectResult(destino, permanent: false, preserveMethod: true);
    }
}
