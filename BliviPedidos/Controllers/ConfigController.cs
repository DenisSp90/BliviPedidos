using BliviPedidos.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BliviPedidos.Controllers;

[Authorize]
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
    public IActionResult Save(PixAppSettingsModel model)
    {
        var jsonPath = Path.Combine(_env.ContentRootPath, "appsettings.json");
        var json = System.IO.File.ReadAllText(jsonPath);

        dynamic jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
        jsonObj["PixAppSettings"]["Responsavel"] = model.Responsavel;
        jsonObj["PixAppSettings"]["PixTipo"] = model.PixTipo;
        jsonObj["PixAppSettings"]["PixChave"] = model.PixChave;
        jsonObj["PixAppSettings"]["PixCity"] = model.PixCity;


        string output = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObj, Newtonsoft.Json.Formatting.Indented);
        System.IO.File.WriteAllText(jsonPath, output);

        return RedirectToAction("Index");
    }
}
