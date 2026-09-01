using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using BliviPedidos.Data;
using BliviPedidos.Models;
using BliviPedidos.Services.Interfaces;
using iText.Kernel.Geom;
using Microsoft.EntityFrameworkCore;
using iText.Kernel.Colors;

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

    public byte[] GerarRelatorioPedidosAtivos(IEnumerable<Pedido> pedidos, string tituloRelatorio, string[] configuracoesRelatorio)
    {
        using (var stream = new MemoryStream())
        {
            var campoOrdenacao = configuracoesRelatorio[0];  // Campo para ordenação (ex: "ID", "DataPedido")
            var tipoOrdenacao = configuracoesRelatorio[1];   // Crescente ou Decrescente

            PdfWriter writer = new PdfWriter(stream);
            PdfDocument pdf = new PdfDocument(writer);

            // Configurar a página como horizontal (landscape)
            pdf.SetDefaultPageSize(PageSize.A4.Rotate());

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

            // Criação da tabela para os pedidos (largura ajustada)
            Table table = new Table(new float[] { 1, 3, 2, 2, 2, 3 });
            table.SetWidth(UnitValue.CreatePercentValue(100));

            // Cabeçalhos da Tabela
            table.AddHeaderCell(new Cell().Add(new Paragraph("Número do Pedido (ID)")).SetBorder(Border.NO_BORDER).SetBold());
            table.AddHeaderCell(new Cell().Add(new Paragraph("Data do Pedido")).SetBorder(Border.NO_BORDER).SetBold());
            table.AddHeaderCell(new Cell().Add(new Paragraph("Valor do Pedido (R$)")).SetBorder(Border.NO_BORDER).SetBold());
            table.AddHeaderCell(new Cell().Add(new Paragraph("Está Pago")).SetBorder(Border.NO_BORDER).SetBold());
            table.AddHeaderCell(new Cell().Add(new Paragraph("E-mail do Cliente")).SetBorder(Border.NO_BORDER).SetBold());
            table.AddHeaderCell(new Cell().Add(new Paragraph("E-mail do Vendedor")).SetBorder(Border.NO_BORDER).SetBold());

            // Adicionar linhas de pedidos
            foreach (var pedido in pedidos)
            {
                table.AddCell(new Cell().Add(new Paragraph(pedido.Id.ToString())).SetBorder(new SolidBorder(1)));
                table.AddCell(new Cell().Add(new Paragraph(pedido.DataPedido.HasValue
                    ? pedido.DataPedido.Value.ToString("dd/MM/yyyy HH:mm")
                    : "N/A")).SetBorder(new SolidBorder(1)));
                table.AddCell(new Cell().Add(new Paragraph(pedido.ValorTotalPedido.ToString("F2"))).SetBorder(new SolidBorder(1)));
                table.AddCell(new Cell().Add(new Paragraph(pedido.StatusPagamento.ToString())).SetBorder(new SolidBorder(1)));
                table.AddCell(new Cell().Add(new Paragraph(pedido.Cadastro.Cliente.Email)).SetBorder(new SolidBorder(1)));
                table.AddCell(new Cell().Add(new Paragraph(pedido.EmailResponsavel)).SetBorder(new SolidBorder(1)));
            }

            // Adicionar tabela ao documento
            document.Add(table);

            // Fechar o documento
            document.Close();
            return stream.ToArray();
        }
    }

    public byte[] GerarRelatorioPedidosDetalhe(IEnumerable<Pedido> pedidos, string tituloRelatorio, string[] configuracoesRelatorio)
    {
        using (var stream = new MemoryStream())
        {
            PdfWriter writer = new PdfWriter(stream);
            PdfDocument pdf = new PdfDocument(writer);

            // Configurar a página como retrato (portrait) por pedido
            pdf.SetDefaultPageSize(PageSize.A4);

            Document document = new Document(pdf);

            foreach (var pedido in pedidos)
            {
                // Cabeçalho com o título do relatório
                Paragraph header = new Paragraph(tituloRelatorio)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(20)
                    .SetBold()
                    .SetFontColor(ColorConstants.BLUE);  // Define cor do cabeçalho
                document.Add(header);

                // Subtítulo com o ID do pedido
                Paragraph subHeader = new Paragraph($"Detalhes do Pedido #{pedido.Id}")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(14)
                    .SetBold()
                    .SetFontColor(ColorConstants.DARK_GRAY);  // Cor para o subtítulo
                document.Add(subHeader);

                // Informações do cliente
                document.Add(new Paragraph("\nInformações do Cliente:")
                    .SetBold()
                    .SetFontColor(ColorConstants.BLACK));  // Texto do cliente em negrito
                document.Add(new Paragraph($"Nome: {pedido.Cadastro.Cliente.Nome}"));
                document.Add(new Paragraph($"Celular: {pedido.Cadastro.Cliente.Telefone}"));
                document.Add(new Paragraph($"Email: {pedido.Cadastro.Cliente.Email}"));

                // Valor total do pedido
                document.Add(new Paragraph("\nValor Total do Pedido:")
                    .SetBold()
                    .SetFontColor(ColorConstants.BLACK));
                document.Add(new Paragraph($"R$ {pedido.ValorTotalPedido:F2}"));

                // Itens do pedido
                document.Add(new Paragraph("\nItens do Pedido:")
                    .SetBold()
                    .SetFontColor(ColorConstants.BLACK));
                Table itemTable = new Table(new float[] { 3, 1, 2 });
                itemTable.SetWidth(UnitValue.CreatePercentValue(100));

                // Estilizando o cabeçalho da tabela
                itemTable.AddHeaderCell(new Cell().Add(new Paragraph("Produto")).SetBold().SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                itemTable.AddHeaderCell(new Cell().Add(new Paragraph("Quantidade")).SetBold().SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                itemTable.AddHeaderCell(new Cell().Add(new Paragraph("Preço Unitário")).SetBold().SetBackgroundColor(ColorConstants.LIGHT_GRAY));

                foreach (var item in pedido.Itens)
                {
                    itemTable.AddCell(new Paragraph(item.Produto.Nome));
                    itemTable.AddCell(new Paragraph(item.Quantidade.ToString()));
                    itemTable.AddCell(new Paragraph($"R$ {item.PrecoUnitario:F2}"));
                }

                document.Add(itemTable);

                // Informações adicionais do pedido
                document.Add(new Paragraph("\nInformações do Pedido:")
                    .SetBold()
                    .SetFontColor(ColorConstants.BLACK));
                document.Add(new Paragraph($"Pagamento: {pedido.StatusPagamento}"));
                document.Add(new Paragraph($"Data do Pedido: {pedido.DataPedido?.ToString("dd/MM/yyyy HH:mm") ?? "N/A"}"));
                document.Add(new Paragraph($"Data de Pagamento: {pedido.DataPagamento?.ToString("dd/MM/yyyy HH:mm") ?? "N/A"}"));
                document.Add(new Paragraph($"Responsável pela Venda: {pedido.EmailResponsavel}"));

                // Quebra de página para o próximo pedido
                document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
            }

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
