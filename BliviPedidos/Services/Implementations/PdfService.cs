using BliviPedidos.Services.Interfaces;
using DinkToPdf;
using DinkToPdf.Contracts;

namespace BliviPedidos.Services.Implementations;

public class PdfService : IPdfService
{
    private readonly IConverter _converter;

    public PdfService(IConverter converter)
    {
        _converter = converter;
    }

    public async Task<byte[]> GerarPdfAsync(string html)
    {
        return await Task.Run(() =>
        {
            var pdfDoc = new HtmlToPdfDocument()
            {
                GlobalSettings = new GlobalSettings
                {
                    ColorMode = ColorMode.Color,
                    Orientation = Orientation.Portrait,
                    PaperSize = PaperKind.A4
                },
                Objects = {
                    new ObjectSettings {
                        PagesCount = true,
                        HtmlContent = html,
                        WebSettings = { DefaultEncoding = "utf-8" },
                    }
                }
            };
            return _converter.Convert(pdfDoc);
        });
    }
}
