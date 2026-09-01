using AutoMapper;
using BliviPedidos.Data;
using BliviPedidos.Models;
using BliviPedidos.Models.ViewModels;
using BliviPedidos.Services.Interfaces;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using BliviPedidos.Seguranca;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Text.RegularExpressions;

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
    private const long TamanhoMaximoImagem = 5 * 1024 * 1024;
    private readonly string _imagemPasta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "produtos");
    private readonly IConfiguration _configuration;
    private readonly ICategoriaService _categoriaService;
    private readonly ILogger<StoreController> _logger;

    public StoreController(
        ApplicationDbContext context,
        IMapper mapper,
        IPedidoService pedidoService,
        IProdutoService produtoService,
        IEmailEnviarService emailSender,
        IClienteService clienteService,
        IItemPedidoService itemPedidoService,
        IConfiguration configuration,
        ICategoriaService categoriaService,
        ILogger<StoreController> logger)
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
        _categoriaService = categoriaService;
        _logger = logger;
    }

    [HttpPost]
    [Authorize(Policy = PoliticasAutorizacao.Vendas)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AtualizarEstadoPagamento(int idPedido, StatusPagamento statusPagamento)
    {
        try
        {
            var pedido = await _pedidoService.GetPedidoByIdAsync(idPedido);

            if (pedido == null)
                return NotFound();

            await _pedidoService.AtualizarStatusPagamentoAsync(idPedido, statusPagamento);

            if (!string.IsNullOrEmpty(pedido.Cadastro.Nome) && pedido.Cadastro.Nome != "AVULSO")
            {
                var cliente = await _clienteService.ProcurarClienteByTelefoneAsync(pedido.Cadastro.Telefone.Trim());

                //if (!string.IsNullOrEmpty(cliente.Email) && cliente.Email != "email@email.com.br")
                //    await EnviarEmailPagamento(pedido.Cadastro);
            }

            TempData["MensagemSucesso"] = "Situação do pagamento atualizada com sucesso.";
            return RedirectToAction(nameof(PedidoDetalhe), new { id = idPedido });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Falha ao atualizar pagamento do pedido. PedidoId: {PedidoId}, StatusPagamento: {StatusPagamento}",
                idPedido,
                statusPagamento);
            TempData["MensagemErro"] = ex.Message;
            return RedirectToAction(nameof(PedidoDetalhe), new { id = idPedido });
        }
    }

    [HttpPost]
    [Authorize(Policy = PoliticasAutorizacao.Vendas)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AtualizarSituacaoPedido(int idPedido, StatusPedido statusPedido)
    {
        try
        {
            await _pedidoService.AtualizarStatusPedidoAsync(idPedido, statusPedido);
            TempData["MensagemSucesso"] = "Situação do pedido atualizada com sucesso.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Falha ao atualizar situação do pedido. PedidoId: {PedidoId}, StatusPedido: {StatusPedido}",
                idPedido, statusPedido);
            TempData["MensagemErro"] = ex.Message;
        }

        return RedirectToAction(nameof(PedidoDetalhe), new { id = idPedido });
    }

    [HttpPost]
    [Authorize(Policy = PoliticasAutorizacao.Vendas)]
    public async Task<IActionResult> CancelarPedido(int idPedido)
    {
        var pedido = await _pedidoService.GetPedidoByIdAsync(idPedido);

        if (pedido == null)
        {
            return NotFound();
        }

        await _pedidoService.RegistrarCancelamentoPedido(pedido.Id);

        // Retornar o estado atualizado do pedido
        return Ok(new
        {
            StatusPedido = StatusPedido.Cancelado.ToString(),
            StatusPagamento = StatusPagamento.Cancelado.ToString()
        });
    }

    [Authorize(Policy = PoliticasAutorizacao.Estoque)]
    public IActionResult CategoriaCadastro()
    {
        return View(new CategoriaViewModel());
    }

    [HttpPost]
    [Authorize(Policy = PoliticasAutorizacao.Estoque)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CategoriaCadastro([FromForm] CategoriaViewModel model)
    {
        if (ModelState.IsValid)
        {
            Categoria c = _mapper.Map<Categoria>(model);
            if (await _categoriaService.RegistrarCategoriaAsync(c))
                return RedirectToAction(nameof(CategoriaLista), new { filtro = 3 });
            else
                return View(model);
        }
        return View(model);
    }

    [Authorize(Policy = PoliticasAutorizacao.Estoque)]
    public async Task<IActionResult> CategoriaEditar(int id)
    {
        if (id <= 0)
            return NotFound();

        var categoria = await _categoriaService.ProcurarCategoriaAsync(id);
        if (categoria is null)
            return NotFound();

        return View(_mapper.Map<CategoriaViewModel>(categoria));
    }

    [HttpPost]
    [Authorize(Policy = PoliticasAutorizacao.Estoque)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CategoriaEditar([FromForm] CategoriaViewModel model)
    {
        if (model.Id <= 0)
            return NotFound();

        if (!ModelState.IsValid)
            return View(model);

        var categoriaExistente = await _categoriaService.ProcurarCategoriaAsync(model.Id);
        if (categoriaExistente is null)
            return NotFound();

        var categoria = _mapper.Map<Categoria>(model);
        if (await _categoriaService.RegistrarCategoriaAsync(categoria))
            return RedirectToAction(nameof(CategoriaLista), new { filtro = 3 });

        ModelState.AddModelError(string.Empty, "Não foi possível atualizar a categoria.");
        return View(model);
    }

    [Authorize(Policy = PoliticasAutorizacao.Estoque)]
    public async Task<IActionResult> CategoriaLista(int filtro)
    {
        try
        {
            // Valida o filtro, se não for 1, 2 ou 3, redireciona para Home
            if (filtro != 1 && filtro != 2 && filtro != 3)
                return RedirectToAction("Index", "Home");

            IEnumerable<Categoria> categorias = await _categoriaService.GetCategoriaListAsync();

            return View(categorias);
        }
        catch (Exception ex)
        {
            return View("Erro");
        }
    }

    [Authorize(Policy = PoliticasAutorizacao.Vendas)]
    public async Task<IActionResult> ClienteGetByTelefone(string telefone)
    {
        var cliente = await _clienteService.ProcurarClienteByTelefoneAsync(telefone);

        if (cliente != null && cliente.Id != 0)
            return Json(cliente);
        else
            return NotFound();
    }

    [Authorize(Policy = PoliticasAutorizacao.Vendas)]
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

    [Authorize(Policy = PoliticasAutorizacao.Vendas)]
    public async Task<IActionResult> ClienteLista()
    {
        StoreViewModel storeViewModel = new StoreViewModel();

        storeViewModel.Clientes = await _clienteService.GetClientesAsync();

        return View(storeViewModel);
    }

    [Authorize(Policy = PoliticasAutorizacao.Vendas)]
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
    [Authorize(Policy = PoliticasAutorizacao.Vendas)]
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
    [Authorize(Policy = PoliticasAutorizacao.Vendas)]
    public async Task<IActionResult> GetInfoPedidos()
    {
        var numeroPedidosPendentes = await _pedidoService.ContarPedidosPendentesAsync(
            HttpContext.RequestAborted);

        return Json(new { NumeroTotalPedidos = numeroPedidosPendentes });
    }

    [Authorize(Policy = PoliticasAutorizacao.AcessoInterno)]
    public IActionResult Index()
    {
        return View();
    }

    [Route("Store/ItemsSearch/{query?}")]
    [Authorize(Policy = PoliticasAutorizacao.Vendas)]
    public IActionResult ItemsSearch(string query)
    {
        var items = _context.Produto
            .Where(p => p.Nome.Contains(query))
            .Select(p => new { p.Id, p.Nome, p.Quantidade, p.PrecoVenda })
            .ToList();

        return Json(items);
    }

    [Authorize(Policy = PoliticasAutorizacao.Vendas)]
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

    [Authorize(Policy = PoliticasAutorizacao.Vendas)]
    public async Task<IActionResult> PedidoDetalhe(int id)
    {
        try
        {
            if (id == 0)
                return View("Erro");

            Pedido pedido = await _pedidoService.GetPedidoByIdAsync(id);

            if (pedido == null)
                return View("PedidoNaoEncontrado");

            if (pedido.Status == StatusPedido.Carrinho)
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

            string qrCodeValue = pixObj.GetPayLoad();
            string qrCodeImageBase64 = GenerateQrCode(qrCodeValue);

            StoreViewModel viewModel = new StoreViewModel
            {
                Pedido = pedido,
                PixKey = qrCodeValue,
                PixQRCodeUrl = qrCodeImageBase64
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            return View("Erro: " + ex.Message);
        }
    }

    [Authorize(Policy = PoliticasAutorizacao.Vendas)]
    public IActionResult PedidoLista(int filtro, string? busca = null)
    {
        try
        {
            if (filtro != 1 && filtro != 2)
                return RedirectToAction("Index", "Home");

            StoreViewModel storeViewModel = new StoreViewModel();

            IEnumerable<Pedido> pedidos = filtro == 1
                ? _pedidoService.GetListaPedidosRegistrados()
                : _pedidoService.GetListaPedidosRegistradosByEmail(HttpContext.User.Identity!.Name!);

            var buscaNormalizada = busca?.Trim();
            if (!string.IsNullOrWhiteSpace(buscaNormalizada))
            {
                pedidos = pedidos.Where(pedido =>
                    pedido.Id.ToString() == buscaNormalizada ||
                    (!string.IsNullOrWhiteSpace(pedido.CodigoPublico) &&
                     pedido.CodigoPublico.Contains(buscaNormalizada, StringComparison.OrdinalIgnoreCase)));
            }

            storeViewModel.Pedidos = pedidos.OrderByDescending(p => p.Id).ToList();

            storeViewModel.FiltroRegistros = filtro;
            storeViewModel.BuscaPedido = buscaNormalizada;

            return View(storeViewModel);
        }
        catch (Exception ex)
        {
            return View("Erro");
        }
    }

    [Authorize(Policy = PoliticasAutorizacao.Vendas)]
    public async Task<IActionResult> PedidoPreparacao()
    {
        StoreViewModel storeViewModel = new StoreViewModel();

        storeViewModel.Produtos = await _produtoService.GetProdutosAtivosAsync();
        storeViewModel.Pedido = _pedidoService.GetPedido();

        return View(storeViewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PoliticasAutorizacao.Vendas)]
    public async Task<IActionResult> PedidoResumo(Cadastro cadastro)
    {
        var pedido = _pedidoService.GetPedido();

        if (pedido == null || pedido.Itens.Count == 0)
            return RedirectToAction("PedidoPreparacao");

        pedido.ValorTotalPedido = pedido.Itens.Sum(i => i.Subtotal);
        cadastro.Pedido = pedido;

        if (cadastro.VendaAvulsa)
        {
            ModelState.Remove(nameof(Cadastro.Nome));
            ModelState.Remove(nameof(Cadastro.Telefone));
            ModelState.Remove(nameof(Cadastro.Email));

            cadastro.Nome = "Consumidor avulso - loja física";
            cadastro.Telefone = "Não informado";
            cadastro.Email = string.Empty;
            cadastro.Cliente = null;
            cadastro.ClienteId = null;
            pedido.ConsumidorUsuarioId = null;
        }
        else if (!ValidarTelefone(cadastro.Telefone))
            ModelState.AddModelError("Telefone", "O telefone deve estar no formato '55 11 99999-9999'.");

        if (ModelState.IsValid)
        {
            try
            {
                if (!_produtoService.UpdateQuantidade(pedido.Itens))
                {
                    TempData["QuantidadeInsuficienteMessage"] = "A quantidade em estoque é insuficiente para atender ao pedido.";
                    return RedirectToAction("PedidoCadastro", "Store");
                }

                if (!cadastro.VendaAvulsa)
                {
                    var cliente = await _clienteService.ProcurarClienteByTelefoneAsync(cadastro.Telefone);

                    if (cliente == null || cliente.Id == 0)
                        cliente = await _clienteService.RegistrarClienteAsync(cadastro);

                    cadastro.Cliente = _mapper.Map<Cliente>(cliente);
                    cadastro.ClienteId = cliente.Id;
                }

                cadastro.Pedido.Status = StatusPedido.Confirmado;
                cadastro.Pedido.StatusPagamento = StatusPagamento.AguardandoPagamento;
                cadastro.Pedido.CodigoPublico ??= CodigoPublicoPedido.Gerar();
                cadastro.Pedido.EmailResponsavel = HttpContext.User.Identity.Name;
                cadastro.Pedido.DataPedido = DateTime.Now;

                _pedidoService.UpdateCadastro(cadastro);
                if (!cadastro.VendaAvulsa && !string.IsNullOrWhiteSpace(cadastro.Email))
                    await EnviarEmailPedido(cadastro);

                _pedidoService.ClearPedido();

                return RedirectToAction("PedidoDetalhe", new { @id = cadastro.Pedido.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Falha ao concluir pedido. PedidoId: {PedidoId}, TotalItens: {TotalItens}",
                    pedido.Id,
                    pedido.Itens.Count);
                ModelState.AddModelError("", "Ocorreu um erro ao processar o pedido. Por favor, tente novamente mais tarde.");
                return RedirectToAction("Error", "Home", new { message = ex.Message });
            }
        }
        else
        {
            return View("PedidoCadastro", cadastro);
        }
    }

    [Authorize(Policy = PoliticasAutorizacao.Estoque)]
    public IActionResult ProdutoCadastro()
    {
        var viewModel = new ProdutoViewModel
        {
            Categorias = _context.Categoria
                .Where(c => c.IsAtivo) // Opcional: Filtrar apenas categorias ativas
                .OrderBy(c => c.Nome)  // Opcional: Ordenar por nome
                .ToList()
        };

        return View(viewModel);
    }


    [HttpPost]
    [Authorize(Policy = PoliticasAutorizacao.Estoque)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProdutoCadastro([FromForm] ProdutoViewModel model)
    {
        ValidarTipoImagem(model);

        if (!ModelState.IsValid)
        {
            model.Categorias = _context.Categoria
                .Where(c => c.IsAtivo)
                .OrderBy(c => c.Nome)
                .ToList();

            return View(model);
        }

        string? novaFoto = null;
        try
        {
            if (TipoImagemUrl(model))
            {
                model.Foto = NormalizarUrlImagem(model.FotoUrl!);
            }
            else if (model.FotoArquivo is not null)
            {
                novaFoto = await SalvarImagemProdutoAsync(model.FotoArquivo);
                model.Foto = novaFoto;
            }

            var produto = _mapper.Map<Produto>(model);
            if (await _produtoService.RegistrarProdutoAsync(produto))
                return RedirectToAction("ProdutoLista");

            ExcluirImagemLocal(novaFoto);
            model.Foto = null;
            ModelState.AddModelError(string.Empty, "Não foi possível cadastrar o produto.");
        }
        catch (InvalidOperationException ex)
        {
            ExcluirImagemLocal(novaFoto);
            ModelState.AddModelError(nameof(model.FotoArquivo), ex.Message);
        }
        catch
        {
            ExcluirImagemLocal(novaFoto);
            throw;
        }

        model.Categorias = _context.Categoria
            .Where(c => c.IsAtivo)
            .OrderBy(c => c.Nome)
            .ToList();

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
            var produto = _context.Produto.SingleOrDefault(produto => produto.Id == id);
            if (produto is null)
                return Json(new { success = false, errorMessage = "Produto não encontrado." });

            var fotoProduto = produto.Foto;
            _context.Produto.Remove(produto);
            _context.SaveChanges();

            try
            {
                ExcluirImagemLocal(fotoProduto);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "O produto {ProdutoId} foi excluído, mas não foi possível remover sua imagem local {FotoProduto}.",
                    id,
                    fotoProduto);
            }

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, errorMessage = ex.Message });
        }
    }

    [Authorize(Policy = PoliticasAutorizacao.Estoque)]
    public async Task<IActionResult> ProdutoDetalhe(int id)
    {
        var produto = await _produtoService.ProcurarProdutoAsync(id);

        if (produto == null)
            return NotFound();


        return View(produto);
    }

    [Authorize(Policy = PoliticasAutorizacao.Estoque)]
    public async Task<IActionResult> ProdutoEditar(int id)
    {
        var produtoViewModel = await _produtoService.ProcurarProdutoAsync(id);

        if (produtoViewModel == null)
            return NotFound();

        // Carregar as categorias disponíveis
        produtoViewModel.Categorias = _context.Categoria
            .Where(c => c.IsAtivo) // Opcional: Filtrar apenas categorias ativas
            .OrderBy(c => c.Nome)  // Opcional: Ordenar por nome
            .ToList();

        if (EhUrlImagem(produtoViewModel.Foto))
        {
            produtoViewModel.TipoImagem = "Url";
            produtoViewModel.FotoUrl = produtoViewModel.Foto;
        }
        else
        {
            produtoViewModel.TipoImagem = "Upload";
        }

        var cultura = CultureInfo.GetCultureInfo("pt-BR");
        produtoViewModel.PrecoPagoEdicao = produtoViewModel.PrecoPago.ToString("N2", cultura);
        produtoViewModel.PrecoVendaEdicao = produtoViewModel.PrecoVenda.ToString("N2", cultura);

        return View(produtoViewModel);
    }

    [HttpPost]
    [Authorize(Policy = PoliticasAutorizacao.Estoque)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProdutoEditar([FromForm] ProdutoViewModel model)
    {
        string? novaFoto = null;
        string? fotoAnterior = null;
        try
        {
            var produtoExistente = _context.Produto
                .AsNoTracking()
                .SingleOrDefault(produto => produto.Id == model.Id);
            if (produtoExistente is null)
                return NotFound();

            // O caminho enviado pelo formulário não é confiável. A foto anterior
            // sempre vem do registro já isolado pela loja atual no banco.
            fotoAnterior = produtoExistente.Foto;
            model.Foto = fotoAnterior;

            AplicarPrecosEdicao(model);
            ValidarTipoImagem(model);

            if (!ModelState.IsValid)
            {
                model.Categorias = _context.Categoria
                    .Where(c => c.IsAtivo)
                    .OrderBy(c => c.Nome)
                    .ToList();

                return View(model);
            }

            if (TipoImagemUrl(model))
            {
                model.Foto = NormalizarUrlImagem(model.FotoUrl!);
            }
            else if (model.FotoArquivo is not null)
            {
                novaFoto = await SalvarImagemProdutoAsync(model.FotoArquivo);
                model.Foto = novaFoto;
            }

            var produto = _mapper.Map<Produto>(model);

            // Salvar as alterações no banco de dados
            if (await _produtoService.RegistrarProdutoAsync(produto))
            {
                if (!string.Equals(fotoAnterior, model.Foto, StringComparison.OrdinalIgnoreCase))
                    ExcluirImagemLocal(fotoAnterior);
                return RedirectToAction("ProdutoDetalhe", new { id = produto.Id });
            }

            ExcluirImagemLocal(novaFoto);
            model.Foto = fotoAnterior;
            ModelState.AddModelError(string.Empty, "Não foi possível atualizar o produto.");

            model.Categorias = _context.Categoria
                .Where(c => c.IsAtivo)
                .OrderBy(c => c.Nome)
                .ToList();

            return View(model);
        }
        catch (InvalidOperationException ex)
        {
            ExcluirImagemLocal(novaFoto);
            model.Foto = fotoAnterior;
            ModelState.AddModelError(nameof(model.FotoArquivo), ex.Message);

            model.Categorias = _context.Categoria
                .Where(c => c.IsAtivo)
                .OrderBy(c => c.Nome)
                .ToList();

            return View(model);
        }
        catch (Exception ex)
        {
            ExcluirImagemLocal(novaFoto);
            model.Foto = fotoAnterior;
            ModelState.AddModelError("", $"Ocorreu um erro ao processar o pedido: {ex.Message}");

            model.Categorias = _context.Categoria
                .Where(c => c.IsAtivo)
                .OrderBy(c => c.Nome)
                .ToList();

            return View(model);
        }
    }

    private async Task<string> SalvarImagemProdutoAsync(IFormFile arquivo)
    {
        if (arquivo.Length == 0)
            throw new InvalidOperationException("Selecione uma imagem válida.");

        if (arquivo.Length > TamanhoMaximoImagem)
            throw new InvalidOperationException("A imagem deve possuir no máximo 5 MB.");

        var extensao = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
        if (extensao is not (".jpg" or ".jpeg" or ".png" or ".webp"))
            throw new InvalidOperationException("Envie uma imagem JPG, PNG ou WEBP.");

        await using var origem = arquivo.OpenReadStream();
        var cabecalho = new byte[12];
        var lidos = await origem.ReadAsync(cabecalho, Request.HttpContext.RequestAborted);
        var jpeg = lidos >= 3 && cabecalho[0] == 0xFF && cabecalho[1] == 0xD8 && cabecalho[2] == 0xFF;
        var png = lidos >= 8 && cabecalho[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        var webp = lidos >= 12
            && cabecalho[..4].SequenceEqual("RIFF"u8.ToArray())
            && cabecalho[8..12].SequenceEqual("WEBP"u8.ToArray());

        var extensaoCompativel = jpeg && extensao is ".jpg" or ".jpeg"
            || png && extensao == ".png"
            || webp && extensao == ".webp";
        if (!extensaoCompativel)
            throw new InvalidOperationException("O conteúdo do arquivo não corresponde à extensão da imagem.");

        var pastaLoja = Path.Combine(_imagemPasta, _context.LojaIdAtual.ToString());
        Directory.CreateDirectory(pastaLoja);
        var nomeArquivo = $"{Guid.NewGuid():N}{extensao}";
        var caminhoCompleto = Path.Combine(pastaLoja, nomeArquivo);

        try
        {
            origem.Position = 0;
            await using var destino = new FileStream(caminhoCompleto, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await origem.CopyToAsync(destino, Request.HttpContext.RequestAborted);
        }
        catch
        {
            if (System.IO.File.Exists(caminhoCompleto))
                System.IO.File.Delete(caminhoCompleto);
            throw;
        }

        return $"/uploads/produtos/{_context.LojaIdAtual}/{nomeArquivo}";
    }

    private void ValidarTipoImagem(ProdutoViewModel model)
    {
        if (TipoImagemUrl(model))
        {
            if (string.IsNullOrWhiteSpace(model.FotoUrl))
            {
                ModelState.AddModelError(nameof(model.FotoUrl), "Informe a URL da imagem.");
                return;
            }

            if (!EhUrlImagem(model.FotoUrl))
                ModelState.AddModelError(nameof(model.FotoUrl), "A URL da imagem deve começar com http:// ou https://.");

            return;
        }

        if (!string.Equals(model.TipoImagem, "Upload", StringComparison.OrdinalIgnoreCase))
            ModelState.AddModelError(nameof(model.TipoImagem), "Selecione uma origem válida para a imagem.");
    }

    private static bool TipoImagemUrl(ProdutoViewModel model) =>
        string.Equals(model.TipoImagem, "Url", StringComparison.OrdinalIgnoreCase);

    private static bool EhUrlImagem(string? valor) =>
        Uri.TryCreate(valor?.Trim(), UriKind.Absolute, out var url)
        && (url.Scheme == Uri.UriSchemeHttp || url.Scheme == Uri.UriSchemeHttps);

    private static string NormalizarUrlImagem(string valor) => valor.Trim();

    private void AplicarPrecosEdicao(ProdutoViewModel model)
    {
        if (TentarConverterPreco(model.PrecoPagoEdicao, out var precoPago))
            model.PrecoPago = precoPago;
        else
            ModelState.AddModelError(nameof(model.PrecoPagoEdicao), "Informe um preço pago válido.");

        if (TentarConverterPreco(model.PrecoVendaEdicao, out var precoVenda))
            model.PrecoVenda = precoVenda;
        else
            ModelState.AddModelError(nameof(model.PrecoVendaEdicao), "Informe um preço de venda válido.");
    }

    private static bool TentarConverterPreco(string? valor, out decimal preco)
    {
        if (decimal.TryParse(valor, NumberStyles.Number, CultureInfo.GetCultureInfo("pt-BR"), out preco)
            && preco >= 0)
            return true;

        preco = 0;
        return false;
    }

    private void ExcluirImagemLocal(string? caminhoPublico)
    {
        var prefixoLoja = $"/uploads/produtos/{_context.LojaIdAtual}/";
        if (string.IsNullOrWhiteSpace(caminhoPublico)
            || !caminhoPublico.StartsWith(prefixoLoja, StringComparison.OrdinalIgnoreCase))
            return;

        var relativo = caminhoPublico["/uploads/produtos/".Length..]
            .Replace('/', Path.DirectorySeparatorChar);
        var raiz = Path.GetFullPath(_imagemPasta) + Path.DirectorySeparatorChar;
        var caminhoCompleto = Path.GetFullPath(Path.Combine(_imagemPasta, relativo));
        if (caminhoCompleto.StartsWith(raiz, StringComparison.OrdinalIgnoreCase)
            && System.IO.File.Exists(caminhoCompleto))
            System.IO.File.Delete(caminhoCompleto);
    }

    [Authorize(Policy = PoliticasAutorizacao.Estoque)]
    public async Task<IActionResult> ProdutoImportar()
    {
        StoreViewModel storeViewModel = new StoreViewModel();
        storeViewModel.Produtos = await _produtoService.GetProdutosAsync();

        return View(storeViewModel);
    }

    [HttpPost]
    [Authorize(Policy = PoliticasAutorizacao.Estoque)]
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
    [Authorize(Policy = PoliticasAutorizacao.Estoque)]
    public IActionResult ProdutoDownloadListaImportacao()
    {
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/files/ProdutoListaImportacao.xlsx");
        var fileBytes = System.IO.File.ReadAllBytes(filePath);
        var fileName = "ProdutoListaImportacao.xlsx";
        return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [Authorize(Policy = PoliticasAutorizacao.Estoque)]
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
    [Authorize(Policy = PoliticasAutorizacao.Vendas)]
    public UpdateQuantidadeResponse UpdateQuantidade([FromBody] ItemPedido itemPedido)
    {
        return _pedidoService.UpdateQuantidade(itemPedido);
    }

    [HttpPost]
    [Authorize(Policy = PoliticasAutorizacao.Vendas)]
    [ValidateAntiForgeryToken]
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
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao alterar o item {ItemPedidoId} do pedido.", itemPedidoId);
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

    private bool ValidarTelefone(string telefone)
    {
        string padrao = @"^55\s\d{2}\s\d{5}-\d{4}$";
        return Regex.IsMatch(telefone, padrao);
    }

}
