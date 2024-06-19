using AutoMapper;
using BliviPedidos.Data;
using BliviPedidos.Models;
using BliviPedidos.Models.ViewModels;
using BliviPedidos.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BliviPedidos.Controllers
{
    [Authorize]
    public class StoreController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IPedidoService _pedidoService;
        private readonly IProdutoService _produtoService;
        private readonly IEmailSenderService _emailSender;


        public StoreController(
            ApplicationDbContext context,
            IMapper mapper,
            IPedidoService pedidoService,
            IProdutoService produtoService)
        {
            _context = context;
            _mapper = mapper;
            _pedidoService = pedidoService;
            _produtoService = produtoService;
        }

        [HttpPost]
        public IActionResult AtualizarEstadoPagamento(int idPedido, bool pago)
        {
            var pedido = _pedidoService.GetPedidoById(idPedido);

            if (pedido == null)
            {
                return NotFound();
            }

            if (pedido.Pago)
                pedido.DataPagamento = DateTime.Now;
            else
                pedido.DataPagamento = null;

            pedido.Pago = pago;

            _context.SaveChanges();

            return Ok(new { Ativo = pedido.Ativo, Pago = pago });
        }

        [HttpPost]
        public IActionResult CancelarPedido(int idPedido, bool ativo)
        {
            var pedido = _pedidoService.GetPedidoById(idPedido);

            if (pedido == null)
            {
                return NotFound();
            }

            pedido.Ativo = ativo;
            _context.SaveChanges();


            bool pago = pedido.Pago;

            return Ok(new { Ativo = pedido.Ativo, Pago = pago });
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

                StoreViewModel viewModel = new StoreViewModel
                {
                    Pedido = pedido
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
                {
                    return View("Erro");
                }

                Pedido pedido = await _pedidoService.GetPedidoByIdAsync(id);

                if (pedido == null)
                {
                    return View("PedidoNaoEncontrado");
                }

                StoreViewModel viewModel = new StoreViewModel
                {
                    Pedido = pedido
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
                    storeViewModel.Pedidos = _pedidoService.GetListaPedidosAtivos();
                else if (filtro == 2)
                    storeViewModel.Pedidos = _pedidoService.GetListaPedidosAtivosByEmail(HttpContext.User.Identity.Name);

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

            storeViewModel.Produtos = await _produtoService.GetProdutosAsync();
            storeViewModel.Pedido = _pedidoService.GetPedido();


            return View(storeViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PedidoResumo(Cadastro cadastro)
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
                    EnviarEmailPedido(cadastro);
                    _pedidoService.ClearPedido();

                    return RedirectToAction("PedidoDetalhe", new { @id = cadastro.Pedido.Id });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Ocorreu um erro ao processar o pedido. Por favor, tente novamente mais tarde.");
                    return RedirectToAction("Error", "Home");
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
        public async Task<IActionResult> ProdutoCadastro([FromForm] ProdutoViewModel model, IFormFile foto)
        {
            if (ModelState.IsValid)
            {
                Produto p = _mapper.Map<Produto>(model);

                if (foto != null && foto.Length > 0)
                {
                    p.Foto = await ObterBytesDaImagem(foto);
                }

                await _produtoService.RegistrarProdutoAsync(p);

                return RedirectToAction("ProdutoLista");
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
            {
                return NotFound();
            }

            return View(produto);
        }

        public async Task<IActionResult> ProdutoEditar(int id)
        {
            var produto = await _produtoService.ProcurarProdutoAsync(id);

            if (produto == null)
            {
                return NotFound();
            }

            return View(produto);
        }

        [HttpPost]
        public async Task<IActionResult> ProdutoEditar([FromForm] ProdutoViewModel model, IFormFile foto)
        {
            if (ModelState.IsValid)
            {
                Produto p = _mapper.Map<Produto>(model);

                if (foto != null && foto.Length > 0)
                {
                    p.Foto = await ObterBytesDaImagem(foto);
                }

                await _produtoService.RegistrarProdutoAsync(p);

                return RedirectToAction("ProdutoLista");
            }

            return View(model);
        }

        public async Task<IActionResult> ProdutoLista()
        {
            return _context.Produto != null ?
                         View(await _context.Produto.ToListAsync()) :
                         Problem("Entity set 'ApplicationDbContext.Produto'  is null.");

        }

        private async Task<byte[]> ObterBytesDaImagem(IFormFile foto)
        {
            using (var stream = new MemoryStream())
            {
                await foto.CopyToAsync(stream);
                return stream.ToArray();
            }
        }

        [HttpPost]
        public UpdateQuantidadeResponse UpdateQuantidade([FromBody] ItemPedido itemPedido)
        {
            return _pedidoService.UpdateQuantidade(itemPedido);
        }

        private async Task EnviarEmailPedido(Cadastro cadastro)
        {
            string email = cadastro.Email.Trim();
            string assunto = "Novo pedido na loja - Número do pedido " + cadastro.Pedido.Id.ToString();

            string mensagem = $@"
            <p>Solicitação de pedido do site #{cadastro.Pedido.Id}</p>
            <hr />
            <p>Nome do cliente: {cadastro.Nome.Trim()}</p>
            <p>E-mail do cliente: {cadastro.Email.Trim()}</p>
            <p>Telefone do cliente: {cadastro.Telefone.Trim()}</p>
            <p>Endereço do cliente: {cadastro.Endereco.Trim()} - {cadastro.Bairro.Trim()} - {cadastro.Municipio.Trim()} - {cadastro.CEP.Trim()} - {cadastro.UF.Trim()}</p>

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

            string mensagemWhatsApp = $"Novo pedido na loja - Número do pedido {cadastro.Pedido.Id.ToString()}. Detalhes em: {Url.ActionContext.HttpContext.Request.Host.Value.Trim()}/PedidoDetalhe/{cadastro.Pedido.Id.ToString()}";
            //EnviarMensagemWhatsApp(cadastro.Telefone, mensagemWhatsApp);
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
    }
}
