using BliviPedidos.Models;
using Microsoft.AspNetCore.Authorization;
using BliviPedidos.Seguranca;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BliviPedidos.Controllers;

[Authorize(Policy = PoliticasAutorizacao.Administracao)]
public class ConfigController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;

    public ConfigController(IConfiguration configuration, IWebHostEnvironment env)
    {
        _configuration = configuration;
        _env = env;
    }

    public IActionResult Index()
    {
        var settings = new PixAppSettingsModel();
        _configuration.GetSection("PixAppSettings").Bind(settings);

        // Lista de opções para o PixTipo
        ViewBag.PixTipos = new List<SelectListItem>
        {
            new SelectListItem { Value = "CPF", Text = "CPF" },
            new SelectListItem { Value = "CNPJ", Text = "CNPJ" },
            new SelectListItem { Value = "Telefone", Text = "Telefone" },
            new SelectListItem { Value = "Email", Text = "Email" }
        };

        return View(settings);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Save(PixAppSettingsModel model)
    {
        var jsonPath = Path.Combine(_env.ContentRootPath, "appsettings.Local.json");
        var jsonObj = System.IO.File.Exists(jsonPath)
            ? JObject.Parse(System.IO.File.ReadAllText(jsonPath))
            : new JObject();

        jsonObj["PixAppSettings"] = JObject.FromObject(new
        {
            model.Responsavel,
            model.PixTipo,
            model.PixChave,
            model.PixCity
        });

        System.IO.File.WriteAllText(jsonPath, jsonObj.ToString(Formatting.Indented));

        return RedirectToAction("Index");
    }
}
