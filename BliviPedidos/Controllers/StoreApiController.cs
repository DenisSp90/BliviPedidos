using BliviPedidos.Data;
using BliviPedidos.Models.ViewModels;
using BliviPedidos.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Syncfusion.DocIO.DLS;
using Syncfusion.DocIORenderer;
using System.Drawing;
using System.Drawing.Imaging;
using Xceed.Words.NET;
using ZXing;
using ZXing.Common;
using ZXing.Rendering;

namespace BliviPedidos.Controllers;

[ApiController]
[Route("api/StoreApi")]
public class StoreApiController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IProdutoService _produtoService;
    private readonly IPedidoService _pedidoService;


    public StoreApiController(
        ApplicationDbContext context, 
        IProdutoService produtoService,
        IPedidoService pedidoService)
    {
        _context = context;
        _produtoService = produtoService;
        _pedidoService = pedidoService;
    }



    [HttpGet("produtoList")]
    public async Task<IActionResult> GetProdutoList()
    {
        var p = await _produtoService.GetProdutosAsync();
        return Ok(p);
    }

    [HttpGet("produto/{id}")]
    public async Task<IActionResult> GetProduto(int id)
    {
        var produto = await _context.Produto.FindAsync(id);
        if (produto == null)
        {
            return NotFound();
        }
        return Ok(produto);
    }

    [HttpGet("etiqueta/pdf/{produtoId}")]
    public async Task<IActionResult> GerarEtiquetaPdf(int produtoId)
    {
        try
        {
            var produto = await _context.Produto.FindAsync(produtoId);

            if (produto == null)
                return NotFound();

            // Caminho para o template DOCX
            var templatePath = Path.Combine(Directory.GetCurrentDirectory(),
                "wwwroot", "doc", "SL61083 - 6183 - 6283 - 6083 - 2083 - 2283 - 2183 - 4083.docx");

            // Carregar o template DOCX
            using var doc = DocX.Load(templatePath);

            // Gerar o código de barras com tamanho ajustado para caber na etiqueta
            var barcodeWriter = new BarcodeWriter<Bitmap>
            {
                Format = BarcodeFormat.CODE_128,
                Options = new EncodingOptions
                {
                    Width = 200, // Ajuste a largura do código de barras para caber na etiqueta
                    Height = 50  // Ajuste a altura do código de barras
                },
                Renderer = new CustomBitmapRenderer()
            };

            // Assumindo 10 etiquetas por página (2 colunas x 5 linhas)
            for (int i = 1; i <= 10; i++)
            {
                // Preencher os placeholders de texto
                doc.ReplaceText($"{{Id{i}}}", produto.Id.ToString());
                doc.ReplaceText($"{{Nome{i}}}", produto.Nome);
                doc.ReplaceText($"{{PrecoVenda{i}}}", produto.PrecoVenda.ToString("C"));

                // Gerar e salvar a imagem do código de barras
                var barcodeBitmap = barcodeWriter.Write(produto.Id.ToString());
                var barcodePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", $"barcode{i}.png");
                barcodeBitmap.Save(barcodePath, ImageFormat.Png);

                // Inserir a imagem do código de barras na célula correspondente
                var image = doc.AddImage(barcodePath);
                var picture = image.CreatePicture(); // Ajuste o tamanho ao inserir (largura, altura)

                // Substituir o placeholder pela imagem
                var paragraph = doc.Paragraphs.FirstOrDefault(p => p.Text.Contains($"{{Barcode{i}}}"));
                if (paragraph != null)
                {
                    paragraph.ReplaceText($"{{Barcode{i}}}", "");
                    paragraph.AppendPicture(picture); // Adiciona a imagem na posição exata do placeholder
                }
            }

            // Continuar com a conversão e retorno do PDF
            using var docStream = new MemoryStream();
            doc.SaveAs(docStream);

            docStream.Position = 0;

            using (var pdfStream = new MemoryStream())
            {
                using var wordDocument = new WordDocument(docStream, Syncfusion.DocIO.FormatType.Docx);

                using var renderer = new DocIORenderer();
                using var pdfDocument = renderer.ConvertToPDF(wordDocument);

                pdfDocument.Save(pdfStream);
                pdfDocument.Close();

                return File(pdfStream.ToArray(), "application/pdf", "etiqueta.pdf");
            }
        }
        catch (Exception ex)
        {

            ModelState.AddModelError("", ex.Message + "Ocorreu um erro ao processar o pedido. Por favor, tente novamente mais tarde.");
            return RedirectToAction("Error", "Home", new { errorMessage = ex.Message });
        }
    }

    [HttpGet("pedidoListAtivos")]
    public async Task<IActionResult> GetPedidoListAtivos()
    { 
        var pedidos = await _pedidoService.GetListaPedidosAtivosAsync();

        var viewModel = pedidos.Select(p => new PedidoViewModel
        {
            Id = p.Id,
            NomeCliente = p.Cadastro.Nome,
            EmailCliente = p.Cadastro.Email,
            Itens = p.Itens.Select(i => new ItemPedidoViewModel
            {
                NomeProduto = i.Produto.Nome,
                PrecoUnitario = i.PrecoUnitario,
                Quantidade = i.Quantidade
            }).ToList(),
            ValorTotalPedido = p.ValorTotalPedido
        }).ToList();

        return Ok(viewModel);
    }

    [HttpGet("pedido-detalhe/{id}")]
    public async Task<IActionResult> GetPedidoById(int id)
    {
        var pedido = await _pedidoService.GetPedidoByIdAsync(id);  

        if (pedido == null)
            return NotFound(); 

        var viewModel = new PedidoViewModel
        {
            Id = pedido.Id,
            NomeCliente = pedido.Cadastro.Nome,
            CelularCliente = pedido.Cadastro.Telefone,
            EmailCliente = pedido.Cadastro.Email,
            Itens = pedido.Itens.Select(i => new ItemPedidoViewModel
            {
                NomeProduto = i.Produto.Nome,
                PrecoUnitario = i.PrecoUnitario,
                Quantidade = i.Quantidade
            }).ToList(),
            ValorTotalPedido = pedido.ValorTotalPedido, 
            Pago = pedido.Pago,
            DataPedido = pedido.DataPedido, 
            DataPagamento = pedido.DataPagamento, 
            EmailResponsavel = pedido.EmailResponsavel
        };

        return Ok(viewModel);
    }
}

public class CustomBitmapRenderer : IBarcodeRenderer<Bitmap>
{
    public Bitmap Render(BitMatrix matrix, BarcodeFormat format, string content, EncodingOptions options)
    {
        int width = matrix.Width;
        int height = matrix.Height;
        var bitmap = new Bitmap(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bitmap.SetPixel(x, y, matrix[x, y] ? Color.Black : Color.White);
            }
        }
        return bitmap;
    }

    public Bitmap Render(BitMatrix matrix, BarcodeFormat format, string content)
    {
        return Render(matrix, format, content, null);
    }
}