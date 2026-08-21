using BliviPedidos.Services.Implementations;
using BliviPedidos.Seguranca;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BliviPedidos.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = PoliticasAutorizacao.Administracao)]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
