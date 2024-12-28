using System.Globalization;
using System.Text;

namespace BliviPedidos.Models;

public class Pix
{
    private string ID_PAYLOAD_FORMAT_INDICATOR = "00";
    private string ID_MERCHANT_ACCOUNT_INFORMATION = "26";
    private string ID_MERCHANT_ACCOUNT_INFORMATION_GUI = "00";
    private string ID_MERCHANT_ACCOUNT_INFORMATION_KEY = "01";
    private string ID_MERCHANT_ACCOUNT_INFORMATION_DESCRIPTION = "02";
    private string ID_MERCHANT_CATEGORY_CODE = "52";
    private string ID_TRANSACTION_CURRENCY = "53";
    private string ID_TRANSACTION_AMOUNT = "54";
    private string ID_COUNTRY_CODE = "58";
    private string ID_MERCHANT_NAME = "59";
    private string ID_MERCHANT_CITY = "60";
    private string ID_ADDITIONAL_DATA_FIELD_TEMPLATE = "62";
    private string ID_ADDITIONAL_DATA_FIELD_TEMPLATE_TXID = "05";
    private string ID_CRC16 = "63";

    private PixModel _model;

    private Pix() { }

    public Pix(
        string favorecido,
        PixModel.PixType pixType,
        string pixKey,
        string pixCity,
        string idReferenciaPagamento,
        string valorTransacao)
    {
        _model = new PixModel();

        _model.Favorecido = favorecido;
        _model.MyPixType = pixType;
        _model.PixKey = pixKey;
        _model.PixCity = pixCity;
        _model.IdReferenciaPagamento = idReferenciaPagamento;
        _model.ValorTransacao = valorTransacao;
    }

    public string GetPayLoad()
    {
        if (!FillValuesPix(_model))
            return "PIX INVÁLIDO";

        string valorSemFormato = _model.ValorTransacao.Replace("R$ ", "").Replace(".", "").Replace(",", ".");
        string valorFormatado = valorSemFormato.PadLeft(2, '0');

        var _PayloadFormatIndicator = BuildValue(ID_PAYLOAD_FORMAT_INDICATOR, "01");

        var _MerchantAccountInformationGUI = BuildValue(ID_MERCHANT_ACCOUNT_INFORMATION_GUI, _model.DnsReverso);
        var _MerchantAccountInformationChave = BuildValue(ID_MERCHANT_ACCOUNT_INFORMATION_KEY, _model.PixKey);
        var _MerchantAccountInformation = BuildValue(ID_MERCHANT_ACCOUNT_INFORMATION, _MerchantAccountInformationGUI + _MerchantAccountInformationChave);
        var _MerchantCategoryCode = BuildValue(ID_MERCHANT_CATEGORY_CODE, "0000");

        var _TransactionCurrency = BuildValue(ID_TRANSACTION_CURRENCY, "986");
        var _TransactionAmount = BuildValue(ID_TRANSACTION_AMOUNT, valorFormatado);

        var _CountryCode = BuildValue(ID_COUNTRY_CODE, "BR");
        var _MerchantName = BuildValue(ID_MERCHANT_NAME, _model.Favorecido.Length > 25 ? _model.Favorecido.Substring(0, 25) : _model.Favorecido);
        var _MerchantCity = BuildValue(ID_MERCHANT_CITY, _model.PixCity);

        var _AdditionalDataFieldTemplateID = BuildValue(ID_ADDITIONAL_DATA_FIELD_TEMPLATE_TXID, _model.DescricaoPagamento.Length > 25 ? _model.DescricaoPagamento.Substring(0, 25) : _model.DescricaoPagamento);
        var _AdditionalDataFieldTemplate = BuildValue(ID_ADDITIONAL_DATA_FIELD_TEMPLATE, _AdditionalDataFieldTemplateID);

        var _payloadPix =
            _PayloadFormatIndicator +
            _MerchantAccountInformation +
            _MerchantCategoryCode +
            _TransactionCurrency +
            _TransactionAmount +
            _CountryCode +
            _MerchantName +
            _MerchantCity +
            _AdditionalDataFieldTemplate +
            "6304";

        return _payloadPix + CalcCrc16(_payloadPix);
    }

    private string BuildValue(string id, string value)
    {
        return id + value.Length.ToString("00").Replace(",", "") + value;
    }

    private string CalcCrc16(string payload)
    {
        var buffer = System.Text.Encoding.UTF8.GetBytes(payload.ToString().Trim());

        ushort[] crcTable = { 0x0000, 0x1021, 0x2042, 0x3063, 0x4084, 0x50a5, 0x60c6, 0x70e7, 0x8108, 0x9129, 0xa14a, 0xb16b, 0xc18c, 0xd1ad, 0xe1ce, 0xf1ef, 0x1231, 0x0210, 0x3273, 0x2252, 0x52b5, 0x4294, 0x72f7, 0x62d6, 0x9339, 0x8318, 0xb37b, 0xa35a, 0xd3bd, 0xc39c, 0xf3ff, 0xe3de, 0x2462, 0x3443, 0x0420, 0x1401, 0x64e6, 0x74c7, 0x44a4, 0x5485, 0xa56a, 0xb54b, 0x8528, 0x9509, 0xe5ee, 0xf5cf, 0xc5ac, 0xd58d, 0x3653, 0x2672, 0x1611, 0x0630, 0x76d7, 0x66f6, 0x5695, 0x46b4, 0xb75b, 0xa77a, 0x9719, 0x8738, 0xf7df, 0xe7fe, 0xd79d, 0xc7bc, 0x48c4, 0x58e5, 0x6886, 0x78a7, 0x0840, 0x1861, 0x2802, 0x3823, 0xc9cc, 0xd9ed, 0xe98e, 0xf9af, 0x8948, 0x9969, 0xa90a, 0xb92b, 0x5af5, 0x4ad4, 0x7ab7, 0x6a96, 0x1a71, 0x0a50, 0x3a33, 0x2a12, 0xdbfd, 0xcbdc, 0xfbbf, 0xeb9e, 0x9b79, 0x8b58, 0xbb3b, 0xab1a, 0x6ca6, 0x7c87, 0x4ce4, 0x5cc5, 0x2c22, 0x3c03, 0x0c60, 0x1c41, 0xedae, 0xfd8f, 0xcdec, 0xddcd, 0xad2a, 0xbd0b, 0x8d68, 0x9d49, 0x7e97, 0x6eb6, 0x5ed5, 0x4ef4, 0x3e13, 0x2e32, 0x1e51, 0x0e70, 0xff9f, 0xefbe, 0xdfdd, 0xcffc, 0xbf1b, 0xaf3a, 0x9f59, 0x8f78, 0x9188, 0x81a9, 0xb1ca, 0xa1eb, 0xd10c, 0xc12d, 0xf14e, 0xe16f, 0x1080, 0x00a1, 0x30c2, 0x20e3, 0x5004, 0x4025, 0x7046, 0x6067, 0x83b9, 0x9398, 0xa3fb, 0xb3da, 0xc33d, 0xd31c, 0xe37f, 0xf35e, 0x02b1, 0x1290, 0x22f3, 0x32d2, 0x4235, 0x5214, 0x6277, 0x7256, 0xb5ea, 0xa5cb, 0x95a8, 0x8589, 0xf56e, 0xe54f, 0xd52c, 0xc50d, 0x34e2, 0x24c3, 0x14a0, 0x0481, 0x7466, 0x6447, 0x5424, 0x4405, 0xa7db, 0xb7fa, 0x8799, 0x97b8, 0xe75f, 0xf77e, 0xc71d, 0xd73c, 0x26d3, 0x36f2, 0x0691, 0x16b0, 0x6657, 0x7676, 0x4615, 0x5634, 0xd94c, 0xc96d, 0xf90e, 0xe92f, 0x99c8, 0x89e9, 0xb98a, 0xa9ab, 0x5844, 0x4865, 0x7806, 0x6827, 0x18c0, 0x08e1, 0x3882, 0x28a3, 0xcb7d, 0xdb5c, 0xeb3f, 0xfb1e, 0x8bf9, 0x9bd8, 0xabbb, 0xbb9a, 0x4a75, 0x5a54, 0x6a37, 0x7a16, 0x0af1, 0x1ad0, 0x2ab3, 0x3a92, 0xfd2e, 0xed0f, 0xdd6c, 0xcd4d, 0xbdaa, 0xad8b, 0x9de8, 0x8dc9, 0x7c26, 0x6c07, 0x5c64, 0x4c45, 0x3ca2, 0x2c83, 0x1ce0, 0x0cc1, 0xef1f, 0xff3e, 0xcf5d, 0xdf7c, 0xaf9b, 0xbfba, 0x8fd9, 0x9ff8, 0x6e17, 0x7e36, 0x4e55, 0x5e74, 0x2e93, 0x3eb2, 0x0ed1, 0x1ef0 };

        if (buffer == null)
            return "Pix inválido!";

        ushort crc = 0xFFFF;

        for (int i = 0; i < buffer.Length; i++)
        {
            byte c = buffer[i];
            int j = (ushort)(c ^ (crc >> 8)) & 0xFF;
            crc = (ushort)(crcTable[j] ^ (crc << 8));
        }

        var answer = ((crc ^ 0) & 0xFFFF);

        return crc.ToString("X4").ToUpper();
    }

    private bool FillValuesPix(PixModel pixModel)
    {
        pixModel.PixCity = RemoverAcentos(pixModel.PixCity.Trim());
        pixModel.DnsReverso = "br.gov.bcb.pix";
        pixModel.DescricaoPagamento = "ID" + MaskIdTransaction(pixModel.IdReferenciaPagamento);

        if (PixKeyIsValid(pixModel.MyPixType, pixModel.PixKey))
        {
            pixModel.PixKey = FormatPixKey(pixModel.MyPixType, pixModel.PixKey);
            return true;
        }

        return false;
    }

    private string FormatPixKey(PixModel.PixType pixType, string pixKey)
    {
        switch (pixType)
        {
            case PixModel.PixType.cpf:
                // Remover traços e pontos do CPF
                return pixKey.Replace(".", "").Replace("-", "");

            case PixModel.PixType.cnpj:
                // Remover traços, pontos e barras do CNPJ
                return pixKey.Replace(".", "").Replace("-", "").Replace("/", "");

            case PixModel.PixType.email:
                // Não há formatação necessária para e-mail
                return pixKey;

            case PixModel.PixType.celular:
                // Remover todos os caracteres não numéricos e adicionar "+55" no início
                string celularFormatado = "+55" + new string(pixKey.Where(char.IsDigit).ToArray());
                return celularFormatado;

            case PixModel.PixType.chaveAleatoria:
                // Adicionar hífens ao formato correto
                // exemplo de chave - ed2b4306-7a8f-4b32-ab90-32ba838b2d8d
                return pixKey.Insert(8, "-").Insert(13, "-").Insert(18, "-").Insert(23, "-");

            default:
                return pixKey; // Retorna a chave sem formatação se o tipo for inválido
        }
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch { }

        return false;
    }

    private string MaskIdTransaction(string idTransaction)
    {
        string[,] table = new string[10, 2];

        table[0, 0] = "0";
        table[0, 1] = "k";
        table[1, 0] = "1";
        table[1, 1] = "T";
        table[2, 0] = "2";
        table[2, 1] = "e";
        table[3, 0] = "3";
        table[3, 1] = "Y";
        table[4, 0] = "4";
        table[4, 1] = "H";
        table[5, 0] = "5";
        table[5, 1] = "P";
        table[6, 0] = "6";
        table[6, 1] = "p";
        table[7, 0] = "7";
        table[7, 1] = "n";
        table[8, 0] = "8";
        table[8, 1] = "X";
        table[9, 0] = "9";
        table[9, 1] = "M";

        string newId = "";

        foreach (var c in idTransaction)
        {
            for (int n = 0; n < 10; n++)
            {
                if (table[n, 0].Equals(c.ToString()))
                {
                    newId += table[n, 1];
                    break;
                }
            }
        }

        return newId;
    }

    private string RemoverAcentos(string texto)
    {
        string normalizedString = texto.Normalize(NormalizationForm.FormD);
        StringBuilder stringBuilder = new StringBuilder();

        foreach (char c in normalizedString)
        {
            UnicodeCategory unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    private bool PixKeyIsValid(PixModel.PixType pixType, string pixKey)
    {
        switch (pixType)
        {
            case PixModel.PixType.cpf:
                long cpf;
                return pixKey.Length == 11 && long.TryParse(pixKey, out cpf);

            case PixModel.PixType.cnpj:
                long cnpj;
                return pixKey.Length == 14 && long.TryParse(pixKey, out cnpj);

            case PixModel.PixType.email:
                return IsValidEmail(pixKey);

            case PixModel.PixType.celular:
                long celular;
                return pixKey.Length == 11 && long.TryParse(pixKey, out celular);

            case PixModel.PixType.chaveAleatoria:
                return true;

            default:
                return false;
        }
    }
}

public class PixModel
{
    public enum PixType
    {
        cpf,
        cnpj,
        email,
        celular,
        chaveAleatoria
    }

    internal PixModel() { }

    public string ValorTransacao { get; set; }
    public string Favorecido { get; set; }
    public string PixKey { get; set; }
    public string PixCity { get; set; }
    public PixType MyPixType { get; set; }
    public string DnsReverso { get; set; }
    public string DescricaoPagamento { get; set; }
    public string IdReferenciaPagamento { get; set; }
}

public class PixAppSettingsModel
{
    public PixAppSettingsModel()
    {
    }

    public string Responsavel { get; set; }
    public string PixTipo { get; set; }
    public string PixChave { get; set; }
    public string PixCity { get; set; }
    public string PixQRCodeUrl { get; set; }

}