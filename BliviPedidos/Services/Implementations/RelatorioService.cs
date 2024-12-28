using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using BliviPedidos.Data;
using BliviPedidos.Models;
using BliviPedidos.Services.Interfaces;
using iText.Kernel.Geom;

namespace BliviPedidos.Services.Implementations;

public class RelatorioService : IRelatorioService
{
    private readonly ApplicationDbContext _context;

    public RelatorioService(ApplicationDbContext context)
    {
        _context = context;
    }

    public byte[] GerarRelatorioMovimentacaoEstoque(IEnumerable<ProdutoMovimentacao> movimentacoes, string tituloRelatorio)
    {
        using (var stream = new MemoryStream())
        {
            PdfWriter writer = new PdfWriter(stream);

            // Definir o tamanho da página como A4 e orientação horizontal
            PdfDocument pdf = new PdfDocument(writer);
            pdf.SetDefaultPageSize(PageSize.A4.Rotate());

            Document document = new Document(pdf);

            // Cabeçalho centralizado
            Paragraph header = new Paragraph(tituloRelatorio)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontSize(20)
                .SetBold();
            document.Add(header);

            // Espaçamento
            document.Add(new Paragraph("\n"));

            // Criação da tabela
            Table table = new Table(new float[] { 1, 2, 2, 2, 3 });
            table.SetWidth(UnitValue.CreatePercentValue(100));

            // Cabeçalhos da Tabela
            table.AddHeaderCell(new Cell().Add(new Paragraph("Produto")).SetBold());
            table.AddHeaderCell(new Cell().Add(new Paragraph("Data")).SetBold());
            table.AddHeaderCell(new Cell().Add(new Paragraph("Quantidade")).SetBold());
            table.AddHeaderCell(new Cell().Add(new Paragraph("Tipo Movimentação")).SetBold());
            table.AddHeaderCell(new Cell().Add(new Paragraph("Observação")).SetBold());

            // Adicionar linhas de movimentações
            foreach (var movimentacao in movimentacoes)
            {
                table.AddCell(new Cell().Add(new Paragraph(movimentacao.Produto.Nome).SetFontSize(10))); // Reduz a fonte
                table.AddCell(new Cell().Add(new Paragraph(movimentacao.Data.ToString("dd/MM/yyyy HH:mm")).SetFontSize(10))); // Reduz a fonte
                table.AddCell(new Cell().Add(new Paragraph(movimentacao.Quantidade.ToString()).SetFontSize(10))); // Reduz a fonte
                table.AddCell(new Cell().Add(new Paragraph(movimentacao.Tipo).SetFontSize(10))); // Reduz a fonte
                table.AddCell(new Cell().Add(new Paragraph(movimentacao.Observacao).SetFontSize(10))); // Reduz a fonte
            }

            // Adicionar tabela ao documento
            document.Add(table);

            document.Close();
            return stream.ToArray();
        }

    }

    public byte[] GerarRelatorioProdutosComEstoqueBaixo(IEnumerable<Produto> produtos, string tituloRelatorio)
    {
        using (var stream = new MemoryStream())
        {
            PdfWriter writer = new PdfWriter(stream);
            PdfDocument pdf = new PdfDocument(writer);
            Document document = new Document(pdf);

            // Cabeçalho centralizado
            Paragraph header = new Paragraph(tituloRelatorio)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontSize(20)
                .SetBold();
            document.Add(header);

            // Espaçamento
            document.Add(new Paragraph("\n"));

            // Criação da tabela
            Table table = new Table(new float[] { 1, 3, 2, 2 });
            table.SetWidth(UnitValue.CreatePercentValue(100));

            // Cabeçalhos da Tabela
            table.AddHeaderCell(new Cell().Add(new Paragraph("ID")).SetBold());
            table.AddHeaderCell(new Cell().Add(new Paragraph("Nome")).SetBold());
            table.AddHeaderCell(new Cell().Add(new Paragraph("Preço")).SetBold());
            table.AddHeaderCell(new Cell().Add(new Paragraph("Estoque Atual")).SetBold());

            // Adicionar linhas de produtos
            foreach (var produto in produtos)
            {
                table.AddCell(new Cell().Add(new Paragraph(produto.Id.ToString())));
                table.AddCell(new Cell().Add(new Paragraph(produto.Nome)));
                table.AddCell(new Cell().Add(new Paragraph(produto.PrecoVenda.ToString("C"))));
                table.AddCell(new Cell().Add(new Paragraph(produto.Quantidade.ToString())));
            }

            // Adicionar tabela ao documento
            document.Add(table);

            document.Close();
            return stream.ToArray();
        }
    }

    public byte[] GerarRelatorioProdutosEmEstoque(IEnumerable<Produto> produtos, string tituloRelatorio, string[] configuracoesRelatorio)
    {
        using (var stream = new MemoryStream())
        {
            var campoOrdenacao = configuracoesRelatorio[0];  // Código, Nome, Quantidade, etc.
            var tipoOrdenacao = configuracoesRelatorio[1];   // Crescente ou Decrescente


            PdfWriter writer = new PdfWriter(stream);
            PdfDocument pdf = new PdfDocument(writer);
            Document document = new Document(pdf);

            // Cabeçalho centralizado
            Paragraph header = new Paragraph(tituloRelatorio)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontSize(20)
                .SetBold();
            document.Add(header);

            string subtitulo = $"Ordenado por {campoOrdenacao} ({tipoOrdenacao})";
            Paragraph subHeader = new Paragraph(subtitulo)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontSize(12)
                .SetItalic();
            document.Add(subHeader);

            // Espaçamento
            document.Add(new Paragraph("\n"));

            // Criação da tabela
            Table table = new Table(new float[] { 1, 3, 2, 2 });
            table.SetWidth(UnitValue.CreatePercentValue(100));

            // Cabeçalhos da Tabela
            table.AddHeaderCell(new Cell().Add(new Paragraph("ID")).SetBorder(Border.NO_BORDER).SetBold());
            table.AddHeaderCell(new Cell().Add(new Paragraph("Nome")).SetBorder(Border.NO_BORDER).SetBold());
            table.AddHeaderCell(new Cell().Add(new Paragraph("Preço")).SetBorder(Border.NO_BORDER).SetBold());
            table.AddHeaderCell(new Cell().Add(new Paragraph("Quantidade em Estoque")).SetBorder(Border.NO_BORDER).SetBold());

            // Adicionar linhas de produtos
            foreach (var produto in produtos)
            {
                table.AddCell(new Cell().Add(new Paragraph(produto.Id.ToString())).SetBorder(new SolidBorder(1)));
                table.AddCell(new Cell().Add(new Paragraph(produto.Nome)).SetBorder(new SolidBorder(1)));
                table.AddCell(new Cell().Add(new Paragraph(produto.PrecoVenda.ToString("C"))).SetBorder(new SolidBorder(1)));
                table.AddCell(new Cell().Add(new Paragraph(produto.Quantidade.ToString())).SetBorder(new SolidBorder(1)));
            }

            // Adicionar tabela ao documento
            document.Add(table);

            document.Close();
            return stream.ToArray();
        }
    }

    public byte[] GerarRelatorioProdutosParados(IEnumerable<Produto> produtos, string tituloRelatorio)
    {
        throw new NotImplementedException();
    }
}
