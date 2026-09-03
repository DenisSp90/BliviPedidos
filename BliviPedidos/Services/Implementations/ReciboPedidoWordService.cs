using System.Drawing;
using BliviPedidos.Models;
using BliviPedidos.Services.Interfaces;
using Xceed.Document.NET;
using Xceed.Words.NET;

namespace BliviPedidos.Services.Implementations;

public sealed class ReciboPedidoWordService : IReciboPedidoService
{
    private readonly IWebHostEnvironment _environment;

    public ReciboPedidoWordService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public byte[] GerarWordA4(Pedido pedido, Loja loja)
    {
        ArgumentNullException.ThrowIfNull(pedido);
        ArgumentNullException.ThrowIfNull(loja);

        using var stream = new MemoryStream();
        using (var documento = DocX.Create(stream))
        {
            documento.PageWidth = 595;
            documento.PageHeight = 842;
            documento.MarginTop = 48;
            documento.MarginBottom = 48;
            documento.MarginLeft = 54;
            documento.MarginRight = 54;

            AdicionarCabecalho(documento, loja);
            AdicionarIdentificacao(documento, pedido);
            AdicionarItens(documento, pedido);
            AdicionarResumo(documento, pedido);
            AdicionarRodape(documento, loja, pedido);

            documento.Save();
        }

        return stream.ToArray();
    }

    private void AdicionarCabecalho(DocX documento, Loja loja)
    {
        var tabela = documento.AddTable(1, 2);
        tabela.SetWidths(new[] { 95f, 392f });
        tabela.Design = TableDesign.TableNormal;

        var logo = ObterLogoLocal(loja.LogoUrl);
        if (logo is not null)
        {
            var imagem = documento.AddImage(logo);
            tabela.Rows[0].Cells[0].Paragraphs[0]
                .AppendPicture(imagem.CreatePicture(64, 64));
        }

        var marca = tabela.Rows[0].Cells[1].Paragraphs[0];
        marca.Append(loja.Nome).Bold().FontSize(20).Font("Arial").Color(Color.FromArgb(31, 58, 95));
        marca.AppendLine("RECIBO DE PEDIDO").Bold().FontSize(11).Font("Arial").Color(Color.FromArgb(80, 80, 80));

        documento.InsertTable(tabela);
        documento.InsertParagraph().SpacingAfter(4);
    }

    private static void AdicionarIdentificacao(DocX documento, Pedido pedido)
    {
        var codigo = string.IsNullOrWhiteSpace(pedido.CodigoPublico)
            ? pedido.Id.ToString()
            : pedido.CodigoPublico;
        var tabela = documento.AddTable(5, 2);
        tabela.SetWidths(new[] { 243.5f, 243.5f });
        tabela.Design = TableDesign.LightShadingAccent1;

        Preencher(tabela, 0, 0, "Pedido", codigo);
        Preencher(tabela, 0, 1, "Data", pedido.DataPedido?.ToString("dd/MM/yyyy HH:mm") ?? "Não informada");
        Preencher(tabela, 1, 0, "Cliente", pedido.Cadastro?.Nome);
        Preencher(tabela, 1, 1, "Telefone", pedido.Cadastro?.Telefone);
        Preencher(tabela, 2, 0, "E-mail", pedido.Cadastro?.Email);
        Preencher(tabela, 2, 1, "Responsável", pedido.Cadastro?.ResponsavelCerimar);
        Preencher(tabela, 3, 0, "Turma", pedido.Cadastro?.Turma);
        Preencher(tabela, 3, 1, "Vendedor", pedido.EmailResponsavel);
        Preencher(tabela, 4, 0, "Status do pedido", pedido.Status.ToString());
        Preencher(tabela, 4, 1, "Pagamento", pedido.StatusPagamento.ToString());
        documento.InsertTable(tabela);
        documento.InsertParagraph().SpacingAfter(6);
    }

    private static void AdicionarItens(DocX documento, Pedido pedido)
    {
        documento.InsertParagraph("ITENS DO PEDIDO")
            .Bold().Font("Arial").FontSize(12).Color(Color.FromArgb(31, 58, 95)).SpacingAfter(5);

        var tabela = documento.AddTable(pedido.Itens.Count + 1, 5);
        tabela.SetWidths(new[] { 48f, 247f, 55f, 68f, 69f });
        tabela.Design = TableDesign.LightShadingAccent1;
        var titulos = new[] { "Qtd.", "Produto", "Tam.", "Unitário", "Total" };
        for (var coluna = 0; coluna < titulos.Length; coluna++)
            tabela.Rows[0].Cells[coluna].Paragraphs[0].Append(titulos[coluna]).Bold().Font("Arial").FontSize(9);

        for (var indice = 0; indice < pedido.Itens.Count; indice++)
        {
            var item = pedido.Itens[indice];
            var valores = new[]
            {
                item.Quantidade.ToString(),
                item.Produto?.Nome ?? "Produto",
                item.Produto?.Tamanho ?? "-",
                item.PrecoUnitario.ToString("C"),
                item.Subtotal.ToString("C")
            };
            for (var coluna = 0; coluna < valores.Length; coluna++)
                tabela.Rows[indice + 1].Cells[coluna].Paragraphs[0].Append(valores[coluna]).Font("Arial").FontSize(9);
        }

        documento.InsertTable(tabela);
    }

    private static void AdicionarResumo(DocX documento, Pedido pedido)
    {
        var total = pedido.Itens.Sum(item => item.Subtotal);
        documento.InsertParagraph($"TOTAL: {total:C}")
            .Bold().Font("Arial").FontSize(15).Alignment = Alignment.right;
    }

    private static void AdicionarRodape(DocX documento, Loja loja, Pedido pedido)
    {
        documento.InsertParagraph("COMPROVANTE DO PEDIDO")
            .Bold().Font("Arial").FontSize(10).Color(Color.FromArgb(31, 58, 95)).SpacingBefore(14);
        documento.InsertParagraph("Este documento confirma os itens e valores registrados no pedido. Não substitui nota fiscal.")
            .Font("Arial").FontSize(9).Color(Color.FromArgb(90, 90, 90));

        var contatos = new[] { loja.EmailContato, loja.Whatsapp, loja.InstagramUrl }
            .Where(valor => !string.IsNullOrWhiteSpace(valor));
        documento.InsertParagraph(string.Join("  |  ", contatos))
            .Font("Arial").FontSize(9).Color(Color.FromArgb(90, 90, 90)).Alignment = Alignment.center;

        documento.AddFooters();
        documento.Footers.Odd.Paragraphs[0]
            .Append($"{loja.Nome} • Pedido #{pedido.CodigoPublico ?? pedido.Id.ToString()} • Gerado em {DateTime.Now:dd/MM/yyyy HH:mm}")
            .Font("Arial").FontSize(8).Color(Color.Gray);
        documento.Footers.Odd.Paragraphs[0].Alignment = Alignment.center;
    }

    private static void Preencher(Table tabela, int linha, int coluna, string rotulo, string? valor)
    {
        var paragrafo = tabela.Rows[linha].Cells[coluna].Paragraphs[0];
        paragrafo.Append($"{rotulo}: ").Bold().Font("Arial").FontSize(9);
        paragrafo.Append(string.IsNullOrWhiteSpace(valor) ? "-" : valor).Font("Arial").FontSize(9);
    }

    private string? ObterLogoLocal(string? logoUrl)
    {
        if (string.IsNullOrWhiteSpace(logoUrl) || !logoUrl.StartsWith('/'))
            return null;

        var raiz = Path.GetFullPath(_environment.WebRootPath);
        var caminho = Path.GetFullPath(Path.Combine(raiz, logoUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));
        return caminho.StartsWith(raiz, StringComparison.OrdinalIgnoreCase) && File.Exists(caminho)
            ? caminho
            : null;
    }
}
