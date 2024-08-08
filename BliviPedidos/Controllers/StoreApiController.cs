using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;
using BliviPedidos.Data;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using ZXing;
using ZXing.Common;
using ZXing.Rendering;
using System.Drawing;
using System.Drawing.Imaging;
using Xceed.Words.NET;

namespace BliviPedidos.Controllers;

[ApiController]
[Route("api/StoreApi")]
public class StoreApiController : Controller
{
    private readonly ApplicationDbContext _context;

    public StoreApiController(ApplicationDbContext context)
    {
        _context = context;
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
        var produto = await _context.Produto.FindAsync(produtoId);

        if (produto == null)
            return NotFound();

        // Caminho para o template DOCX
        var templatePath = Path.Combine(Directory.GetCurrentDirectory(),
            "wwwroot", "doc", "SL61083 - 6183 - 6283 - 6083 - 2083 - 2283 - 2183 - 4083.docx");

        // Carregar o template DOCX
        using var doc = DocX.Load(templatePath);

        // Preencher o template com os dados do produto
        doc.ReplaceText("{Nome}", produto.Nome);
        doc.ReplaceText("{PrecoVenda}", produto.PrecoVenda.ToString("C"));

        // Gerar o código de barras
        var barcodeWriter = new BarcodeWriter<Bitmap>
        {
            Format = BarcodeFormat.CODE_128,
            Options = new EncodingOptions
            {
                Width = 300,
                Height = 150
            },
            Renderer = new CustomBitmapRenderer()
        };
        var barcodeBitmap = barcodeWriter.Write(produto.Id.ToString());
        var barcodePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "barcode.png");
        barcodeBitmap.Save(barcodePath, ImageFormat.Png);

        // Inserir a imagem do código de barras no DOCX
        var imageParagraph = doc.InsertParagraph();
        var image = doc.AddImage(barcodePath);
        var picture = image.CreatePicture();
        imageParagraph.AppendPicture(picture);

        // Salvar o DOCX em um MemoryStream
        using var docStream = new MemoryStream();
        doc.SaveAs(docStream);

        // Converter o DOCX para PDF usando o OpenXmlPowerTools
        docStream.Position = 0;
        byte[] pdfBytes;

        using (var pdfStream = new MemoryStream())
        {
            var pdfWriter = new PdfWriter(pdfStream);
            var pdfDocument = new PdfDocument(pdfWriter);
            var document = new Document(pdfDocument);

            //// Aqui você pode adicionar conteúdo adicional ao PDF ou processar conforme necessário
            //document.Add(new Paragraph("Este PDF foi gerado a partir de um documento Word."));

            document.Close();
            pdfBytes = pdfStream.ToArray();
        }

        // Retornar o PDF na resposta
        return File(pdfBytes, "application/pdf", "etiqueta.pdf");
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