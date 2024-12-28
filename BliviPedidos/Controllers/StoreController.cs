using AutoMapper;
using BliviPedidos.Data;
using BliviPedidos.Models;
using BliviPedidos.Models.ViewModels;
using BliviPedidos.Services.Interfaces;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;

namespace BliviPedidos.Controllers;

[Authorize]
public class StoreController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IItemPedidoService _itemPedidoService;
    private readonly IPedidoService _pedidoService;
    private readonly IProdutoService _produtoService;
    private readonly IEmailEnviarService _emailSender;
    private readonly IClienteService _clienteService;
    private readonly string _imagemPasta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "imagens/produtos");
    private readonly IConfiguration _configuration;

    public StoreController(
        ApplicationDbContext context,
        IMapper mapper,
        IPedidoService pedidoService,
        IProdutoService produtoService,
        IEmailEnviarService emailSender,
        IClienteService clienteService,
        IItemPedidoService itemPedidoService,
        IConfiguration configuration)
    {
        _context = context;
        _mapper = mapper;
        _pedidoService = pedidoService;
        _produtoService = produtoService;
        _emailSender = emailSender;
        _clienteService = clienteService;

        if (!Directory.Exists(_imagemPasta))
        {
            Directory.CreateDirectory(_imagemPasta);
        }

        _itemPedidoService = itemPedidoService;
        _configuration = configuration;
    }

    [HttpPost]
    public async Task<IActionResult> AtualizarEstadoPagamento(int idPedido, bool pago)
    {
        try
        {
            var pedido = await _pedidoService.GetPedidoByIdAsync(idPedido);

            if (pedido == null)
                return NotFound();

            await _pedidoService.AtualizarStatusPagamentoAsync(idPedido, pago);

            if (!string.IsNullOrEmpty(pedido.Cadastro.Nome) && pedido.Cadastro.Nome != "AVULSO")
            {
                var cliente = await _clienteService.ProcurarClienteByTelefoneAsync(pedido.Cadastro.Telefone.Trim());

                //if (!string.IsNullOrEmpty(cliente.Email) && cliente.Email != "email@email.com.br")
                //    await EnviarEmailPagamento(pedido.Cadastro);
            }

            return Ok(new { Ativo = pedido.Ativo, Pago = pago });
        }
        catch (Exception ex)
        {

            return BadRequest(ex.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> CancelarPedido(int idPedido, bool ativo)
    {
        var pedido = await _pedidoService.GetPedidoByIdAsync(idPedido);

        if (pedido == null)
        {
            return NotFound();
        }

        await _pedidoService.RegistrarCancelamentoPedido(pedido.Id);

        // Retornar o estado atualizado do pedido
        return Ok(new { Ativo = pedido.Ativo, Pago = pedido.Pago });
    }

    public async Task<IActionResult> ClienteGetByTelefone(string telefone)
    {
        // Lógica para buscar o cliente pelo telefone
        // Isso pode envolver uma consulta ao banco de dados
        var cliente = await _clienteService.ProcurarClienteByTelefoneAsync(telefone);

        if (cliente != null)
            return Json(cliente); // Retorna os dados do cliente como JSON
        else
            return NotFound(); // Retorna um status 404 se o cliente não for encontrado
    }

    public async Task<IActionResult> ClienteDetalhe(int id)
    {
        try
        {
            if (id == 0)
                return View("Erro");

            var sViewModel = new StoreViewModel();

            sViewModel.Cliente = await _clienteService.GetClienteByIdAsync(id);

            return View(sViewModel);
        }
        catch (Exception)
        {

            throw;
        }
    }

    public async Task<IActionResult> ClienteLista()
    {
        StoreViewModel storeViewModel = new StoreViewModel();

        storeViewModel.Clientes = await _clienteService.GetClientesAsync();

        return View(storeViewModel);
    }

    public async Task<IActionResult> CuponFiscal(int id)
    {
        try
        {
            if (id == 0)
            {
                return View("Erro");
            }

            Pedido pedido = await _pedidoService.GetPedidoByIdAsync(id);

            if (pedido == null)
            {
                return View("PedidoNaoEncontrado");
            }

            var responsavel = _configuration["PixAppSettings:Responsavel"];
            var pixTipo = _configuration["PixAppSettings:PixTipo"];
            var pixChave = _configuration["PixAppSettings:PixChave"];
            var pixCity = _configuration["PixAppSettings:PixCity"];

            PixModel.PixType pixType = PixModel.PixType.cnpj;

            switch (pixTipo)
            {
                case "CPF":
                    pixType = PixModel.PixType.cpf;
                    break;

                case "CNPJ":
                    pixType = PixModel.PixType.cnpj;
                    break;

                case "Telefone":
                    pixType = PixModel.PixType.celular;
                    break;

                case "Email":
                    pixType = PixModel.PixType.email;
                    break;

                default:
                    pixType = PixModel.PixType.chaveAleatoria;
                    break;
            }

            Pix pixObj = new Pix(
               responsavel,
               pixType,
               pixChave,
               pixCity,
               "_boleto.NumeroTitulo",
               String.Format("{0:C}", pedido.ValorTotalPedido));           

            string qrCodeValue = pixObj.GetPayLoad();
            string qrCodeImageBase64 = GenerateQrCode(qrCodeValue);

            StoreViewModel viewModel = new StoreViewModel
            {
                Pedido = pedido,
                PixKey = qrCodeValue,
                PixQRCodeUrl = qrCodeImageBase64 // Substitua pela URL real do QR code
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            return View("Erro");
        }
    }

    [HttpPost]
    [Route("Store/Carrinho/{produtoId?}")]
    public IActionResult Carrinho(int produtoId)
    {
        if (produtoId > 0)
        {
            _pedidoService.AddItem(produtoId);

            List<ItemPedido> items = _pedidoService.GetPedido().Itens;

            CarrinhoViewModel carrinhoViewModel = new CarrinhoViewModel(items);

            return Ok(new UpdateQuantidadeResponse(items, carrinhoViewModel));
        }
        else
        {
            return BadRequest("ID do produto inválido.");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetInfoPedidos()
    {
        var listaPedidosAtivos = _pedidoService.GetListaPedidosAtivosByEmail(HttpContext.User.Identity.Name);
        var numeroPedidosNaoPagos = listaPedidosAtivos.Count(pedido => !pedido.Pago);
        var numeroTotalPedidos = listaPedidosAtivos.Count;

        return Json(new { NumeroTotalPedidos = numeroTotalPedidos, NumeroPedidosNaoPagos = numeroPedidosNaoPagos });
    }

    public IActionResult Index()
    {
        return View();
    }

    [Route("Store/ItemsSearch/{query?}")]
    public IActionResult ItemsSearch(string query)
    {
        var items = _context.Produto
            .Where(p => p.Nome.Contains(query))
            .Select(p => new { p.Id, p.Nome, p.Quantidade, p.PrecoVenda })
            .ToList();

        return Json(items);
    }

    public IActionResult PedidoCadastro()
    {
        StoreViewModel storeViewModel = new StoreViewModel();

        var pedido = _pedidoService.GetPedido();

        if (pedido == null || pedido.Itens.Count == 0)
            return RedirectToAction("PedidoPreparacao");

        if (TempData.ContainsKey("QuantidadeInsuficienteMessage"))
        {
            ViewBag.QuantidadeInsuficienteMessage = TempData["QuantidadeInsuficienteMessage"];
        }

        pedido.ValorTotalPedido = pedido.Itens.Sum(item => item.Subtotal);

        return View(pedido.Cadastro);
    }

    public async Task<IActionResult> PedidoDetalhe(int id)
    {
        try
        {
            if (id == 0)
                return View("Erro");

            Pedido pedido = await _pedidoService.GetPedidoByIdAsync(id);

            if (pedido == null)
                return View("PedidoNaoEncontrado");

            if (!pedido.Ativo)
                return RedirectToAction("PedidoLista", "Store");

            var responsavel = _configuration["PixAppSettings:Responsavel"];
            var pixTipo = _configuration["PixAppSettings:PixTipo"];
            var pixChave = _configuration["PixAppSettings:PixChave"];
            var pixCity = _configuration["PixAppSettings:PixCity"];

            PixModel.PixType pixType = PixModel.PixType.cnpj;
            
            switch (pixTipo)
            {
                case "CPF":
                    pixType = PixModel.PixType.cpf;
                    break;

                case "CNPJ":
                    pixType = PixModel.PixType.cnpj;
                    break;

                case "Telefone":
                    pixType = PixModel.PixType.celular;
                    break;

                case "Email":
                    pixType = PixModel.PixType.email;
                    break;

                default:
                    pixType = PixModel.PixType.chaveAleatoria;
                    break;
            }

            Pix pixObj = new Pix(
               responsavel,
               pixType,
               pixChave,
               pixCity,
               "_boleto.NumeroTitulo",
               String.Format("{0:C}", pedido.ValorTotalPedido));

            //Pix pixObj = new Pix(
            //    "COLEGIO CERIMAR",
            //    pixType,
            //    "01904650000124",
            //    "SAOPAULO",
            //    "_boleto.NumeroTitulo",
            //    String.Format("{0:C}", pedido.ValorTotalPedido));

            string qrCodeValue = pixObj.GetPayLoad();
            string qrCodeImageBase64 = GenerateQrCode(qrCodeValue);

            StoreViewModel viewModel = new StoreViewModel
            {
                Pedido = pedido,
                PixKey = qrCodeValue,
                PixQRCodeUrl = qrCodeImageBase64 // Substitua pela URL real do QR code
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            return View("Erro: " + ex.Message);
        }
    }

    public IActionResult PedidoLista(int filtro)
    {
        try
        {
            if (filtro != 1 && filtro != 2)
                return RedirectToAction("Index", "Home");

            StoreViewModel storeViewModel = new StoreViewModel();

            if (filtro == 1)
                storeViewModel.Pedidos = _pedidoService.GetListaPedidosAtivos()
                    .OrderByDescending(p => p.Id).ToList();
            else if (filtro == 2)
                storeViewModel.Pedidos = _pedidoService.GetListaPedidosAtivosByEmail(HttpContext.User.Identity.Name)
                    .OrderByDescending(p => p.Id).ToList();

            storeViewModel.FiltroRegistros = filtro;

            return View(storeViewModel);
        }
        catch (Exception ex)
        {
            return View("Erro");
        }
    }

    public async Task<IActionResult> PedidoPreparacao()
    {
        StoreViewModel storeViewModel = new StoreViewModel();

        storeViewModel.Produtos = await _produtoService.GetProdutosAtivosAsync();
        storeViewModel.Pedido = _pedidoService.GetPedido();

        return View(storeViewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PedidoResumo(Cadastro cadastro)
    {
        var pedido = _pedidoService.GetPedido();

        pedido.ValorTotalPedido = pedido.Itens.Sum(i => i.Subtotal);

        if (pedido == null || pedido.Itens.Count == 0)
            return RedirectToAction("PedidoPreparacao");

        cadastro.Pedido = pedido;

        if (ModelState.IsValid)
        {
            try
            {
                if (!_produtoService.UpdateQuantidade(pedido.Itens))
                {
                    TempData["QuantidadeInsuficienteMessage"] = "A quantidade em estoque é insuficiente para atender ao pedido.";
                    return RedirectToAction("PedidoCadastro", "Store");
                }

                cadastro.Pedido.Ativo = true;
                cadastro.Pedido.EmailResponsavel = HttpContext.User.Identity.Name;
                cadastro.Pedido.DataPedido = DateTime.Now;

                _pedidoService.UpdateCadastro(cadastro);

                //if (!string.IsNullOrEmpty(cadastro.Email) && cadastro.Email != "email@email.com.br")
                //    await EnviarEmailPedido(cadastro);

                if (!string.IsNullOrEmpty(cadastro.Nome) && cadastro.Nome != "AVULSO")
                    await _clienteService.RegistrarClienteAsync(cadastro);

                _pedidoService.ClearPedido();

                return RedirectToAction("PedidoDetalhe", new { @id = cadastro.Pedido.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Ocorreu um erro ao processar o pedido. Por favor, tente novamente mais tarde.");
                return RedirectToAction(ex.Message, "Home");
            }
        }
        else
        {
            return View("PedidoCadastro", cadastro);
        }
    }

    public IActionResult ProdutoCadastro()
    {
        return View(new ProdutoViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> ProdutoCadastro([FromForm] ProdutoViewModel model)
    {
        if (ModelState.IsValid)
        {
            Produto p = _mapper.Map<Produto>(model);

            if (await _produtoService.RegistrarProdutoAsync(p))
                return RedirectToAction("ProdutoLista");
            else
                return View(model);
        }

        return View(model);
    }

    [HttpPost]
    public ActionResult ProdutoDelete(int id)
    {
        bool produtoVinculadoPedido = _produtoService.VerificarProdutoVinculadoPedido(id);

        if (produtoVinculadoPedido)
        {
            return Json(new { success = false, errorMessage = "Não é possível excluir o produto pois está vinculado a um item de pedido." });
        }

        try
        {
            var produto = _context.Produto.Find(id);
            _context.Produto.Remove(produto);
            _context.SaveChanges();

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, errorMessage = ex.Message });
        }
    }

    public async Task<IActionResult> ProdutoDetalhe(int id)
    {
        var produto = await _produtoService.ProcurarProdutoAsync(id);

        if (produto == null)
            return NotFound();


        return View(produto);
    }

    public async Task<IActionResult> ProdutoEditar(int id)
    {
        var produto = await _produtoService.ProcurarProdutoAsync(id);

        if (produto == null)
            return NotFound();

        if (string.IsNullOrEmpty(produto.Foto))
            produto.Foto = "/img/default.png";

        return View(produto);
    }

    [HttpPost]
    public async Task<IActionResult> ProdutoEditar([FromForm] ProdutoViewModel model)
    {
        try
        {
            if (ModelState.IsValid)
            {
                Produto p = _mapper.Map<Produto>(model);
                await _produtoService.RegistrarProdutoAsync(p);

                return RedirectToAction("ProdutoDetalhe", new { id = p.Id });
            }

            return View(model);
        }
        catch (Exception ex)
        {

            ModelState.AddModelError("", ex.Message + "Ocorreu um erro ao processar o pedido. Por favor, tente novamente mais tarde.");
            return RedirectToAction("Error", "Home");
        }
    }

    public async Task<IActionResult> ProdutoImportar()
    {
        StoreViewModel storeViewModel = new StoreViewModel();
        storeViewModel.Produtos = await _produtoService.GetProdutosAsync();

        return View(storeViewModel);
    }

    [HttpPost]
    public async Task<IActionResult> ProdutoImportarUpload(IFormFile file)
    {
        if (file != null && file.Length > 0)
        {
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".xlsx")
            {
                return BadRequest(new { message = "Invalid file type. Only .xlsx files are allowed." });
            }

            var path = Path.Combine(Directory.GetCurrentDirectory(), "uploads", file.FileName);

            if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "uploads")))
            {
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "uploads"));
            }

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var produtos = new List<Produto>();

            using (var workbook = new XLWorkbook(path))
            {
                var worksheet = workbook.Worksheet(1);
                var rows = worksheet.RowsUsed().Skip(1);

                foreach (var row in rows)
                {
                    var produto = new Produto
                    {
                        Codigo = row.Cell(1).GetValue<string>(),
                        Nome = row.Cell(2).GetValue<string>(),
                        PrecoPago = row.Cell(3).GetValue<decimal>(),
                        PrecoVenda = row.Cell(4).GetValue<decimal>(),
                        Quantidade = row.Cell(5).GetValue<int>(),
                        Tamanho = row.Cell(6).GetValue<string>(),
                        CodeBar = row.Cell(7).GetValue<string>(),
                        Foto = "/img/default.png"
                    };

                    if (produto.PrecoPago == 0 || produto.PrecoVenda == 0 || produto.Quantidade == 0 || string.IsNullOrWhiteSpace(produto.Nome))
                    {
                        produto.Nome += " não inserido";
                        continue;
                    }

                    bool produtoJaExiste = await _produtoService.VerificarExistenciaProdutoNoBanco(produto.Nome.Trim(), produto.PrecoPago);
                    if (produtoJaExiste)
                    {
                        produto.Nome += " já registrado";
                        continue;
                    }
                    else
                    {
                        await _produtoService.RegistrarProdutoAsync(produto);
                    }

                    produtos.Add(produto);
                }
            }

            var jsonResult = Newtonsoft.Json.JsonConvert.SerializeObject(produtos);
            return Ok(jsonResult);
        }

        return BadRequest(new { message = "Invalid file." });
    }

    [HttpGet]
    public IActionResult ProdutoDownloadListaImportacao()
    {
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/files/ProdutoListaImportacao.xlsx");
        var fileBytes = System.IO.File.ReadAllBytes(filePath);
        var fileName = "ProdutoListaImportacao.xlsx";
        return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    public async Task<IActionResult> ProdutoLista(int filtro)
    {
        try
        {
            // Valida o filtro, se não for 1, 2 ou 3, redireciona para Home
            if (filtro != 1 && filtro != 2 && filtro != 3)
                return RedirectToAction("Index", "Home");

            IEnumerable<Produto> produtos;

            // Filtra os produtos com base no valor de 'filtro'
            if (filtro == 1) // Produtos Ativos
            {
                produtos = await _produtoService.GetProdutosAtivosAsync();
            }
            else if (filtro == 2) // Produtos Desativados
            {
                produtos = await _produtoService.GetProdutosDesativadosAsync();
            }
            else // Todos os Produtos
            {
                produtos = await _produtoService.GetProdutosAsync();
            }

            // Retorna a view com a lista de produtos filtrada
            return View(produtos);
        }
        catch (Exception ex)
        {
            // Retorna a view de erro em caso de exceção
            return View("Erro");
        }
    }

    [HttpPost]
    [Route("Store/UploadImagem/{produtoId}")]
    public async Task<IActionResult> UploadImagem(string produtoId)
    {
        if (Request.Form.Files.Count > 0 && !string.IsNullOrEmpty(produtoId))
        {
            var arquivo = Request.Form.Files[0];

            var nomeArquivo = Path.GetFileNameWithoutExtension(arquivo.FileName);
            var extensaoArquivo = Path.GetExtension(arquivo.FileName);
            var nomeArquivoNovo = $"{nomeArquivo}_{System.Guid.NewGuid()}{extensaoArquivo}";

            var caminhoArquivo = Path.Combine(_imagemPasta, nomeArquivoNovo);

            using (var stream = new FileStream(caminhoArquivo, FileMode.Create))
            {
                await arquivo.CopyToAsync(stream);
            }

            var urlImagem = $"/imagens/produtos/{nomeArquivoNovo}";

            await _produtoService.AtualizarImagemProdutoAsync(int.Parse(produtoId), urlImagem);

            return Json(new { imagemUrl = urlImagem });
        }

        return BadRequest("Arquivo ou ID do produto não encontrados.");
    }

    [HttpPost]
    public UpdateQuantidadeResponse UpdateQuantidade([FromBody] ItemPedido itemPedido)
    {
        return _pedidoService.UpdateQuantidade(itemPedido);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateQuantidade2(int itemPedidoId, int produtoId, int quantidade, decimal preco)
    {
        try
        {
            await _itemPedidoService.UpdateItemPedidoAsync(itemPedidoId, produtoId, quantidade, preco);

            return Json(new { success = true });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            // Log.Error(ex.Message); // Caso use um logger
            return StatusCode(500, new { success = false, message = "Ocorreu um erro ao atualizar a quantidade." });
        }
    }

    private async Task EnviarEmailPagamento(Cadastro cadastro)
    {
        string email = cadastro.Email.Trim();
        string assunto = "Pagamento Registrado - Número do pedido " + cadastro.Pedido.Id.ToString();

        string mensagem = $@"
                                <p>Pagamento Registrado de pedido do site #{cadastro.Pedido.Id}</p>
                                <hr />
                                <p>Nome do cliente: {(string.IsNullOrEmpty(cadastro.Nome) ? "<valor não informado>" : cadastro.Nome.Trim())}</p>
                                <p>E-mail do cliente: {(string.IsNullOrEmpty(cadastro.Email) ? "<valor não informado>" : cadastro.Email.Trim())}</p>
                                <p>Telefone do cliente: {(string.IsNullOrEmpty(cadastro.Telefone) ? "<valor não informado>" : cadastro.Telefone.Trim())}</p>
                                <p>Endereço do cliente: 
                                    {(string.IsNullOrEmpty(cadastro.Endereco) ? "<valor não informado>" : cadastro.Endereco.Trim())} - 
                                    {(string.IsNullOrEmpty(cadastro.Bairro) ? "<valor não informado>" : cadastro.Bairro.Trim())} - 
                                    {(string.IsNullOrEmpty(cadastro.Municipio) ? "<valor não informado>" : cadastro.Municipio.Trim())} - 
                                    {(string.IsNullOrEmpty(cadastro.CEP) ? "<valor não informado>" : cadastro.CEP.Trim())} - 
                                    {(string.IsNullOrEmpty(cadastro.UF) ? "<valor não informado>" : cadastro.UF.Trim())}
                                </p>
                                <p>Lista de itens solicitados:</p>
                            ";

        foreach (var item in cadastro.Pedido.Itens)
        {
            mensagem += $@"<p>- {item.Produto.Nome.Trim()} - {item.Quantidade} - R$ {item.PrecoUnitario:N2}</p>";
        }

        mensagem += $@"
            <p>Total: R$ {cadastro.Pedido.ValorTotalPedido.ToString("N2")}</p>";

        if (!string.IsNullOrEmpty(email))
            await TesteEnvioEmail(email, assunto, mensagem);
    }

    private async Task EnviarEmailPedido(Cadastro cadastro)
    {
        string email = cadastro.Email.Trim();
        string assunto = "Novo pedido na loja - Número do pedido " + cadastro.Pedido.Id.ToString();

        string mensagem = $@"
                                <p>Solicitação de pedido do site #{cadastro.Pedido.Id}</p>
                                <hr />
                                <p>Nome do cliente: {(string.IsNullOrEmpty(cadastro.Nome) ? "<valor não informado>" : cadastro.Nome.Trim())}</p>
                                <p>E-mail do cliente: {(string.IsNullOrEmpty(cadastro.Email) ? "<valor não informado>" : cadastro.Email.Trim())}</p>
                                <p>Telefone do cliente: {(string.IsNullOrEmpty(cadastro.Telefone) ? "<valor não informado>" : cadastro.Telefone.Trim())}</p>
                                <p>Endereço do cliente: 
                                    {(string.IsNullOrEmpty(cadastro.Endereco) ? "<valor não informado>" : cadastro.Endereco.Trim())} - 
                                    {(string.IsNullOrEmpty(cadastro.Bairro) ? "<valor não informado>" : cadastro.Bairro.Trim())} - 
                                    {(string.IsNullOrEmpty(cadastro.Municipio) ? "<valor não informado>" : cadastro.Municipio.Trim())} - 
                                    {(string.IsNullOrEmpty(cadastro.CEP) ? "<valor não informado>" : cadastro.CEP.Trim())} - 
                                    {(string.IsNullOrEmpty(cadastro.UF) ? "<valor não informado>" : cadastro.UF.Trim())}
                                </p>
                                <p>Lista de itens solicitados:</p>
                            ";

        foreach (var item in cadastro.Pedido.Itens)
        {
            mensagem += $@"<p>- {item.Produto.Nome.Trim()} - {item.Quantidade} - R$ {item.PrecoUnitario:N2}</p>";
        }

        mensagem += $@"
            <p>Total: R$ {cadastro.Pedido.ValorTotalPedido.ToString("N2")}</p>";

        if (!string.IsNullOrEmpty(email))
            await TesteEnvioEmail(email, assunto, mensagem);
    }

    public async Task TesteEnvioEmail(string email, string assunto, string mensagem)
    {
        try
        {
            await _emailSender.SendEmailAsync(email, assunto, mensagem);
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    private string GenerateQrCode(string text)
    {
        using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
        {
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
            using (QRCode qrCode = new QRCode(qrCodeData))
            {
                using (Bitmap qrCodeImage = qrCode.GetGraphic(2)) // Tamanho do pixel ajustado para 2
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        qrCodeImage.Save(ms, ImageFormat.Png);
                        byte[] byteImage = ms.ToArray();
                        return Convert.ToBase64String(byteImage);
                    }
                }
            }
        }
    }
}
