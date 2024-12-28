using BliviPedidos.Data;
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

    public StoreApiController(
        ApplicationDbContext context, 
        IProdutoService produtoService)
    {
        _context = context;
        _produtoService = produtoService;
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