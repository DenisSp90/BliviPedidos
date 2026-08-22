namespace BliviPedidos.Models;

public enum StatusPagamento
{
    AguardandoPagamento = 0,
    Pago = 1,
    Falhou = 2,
    Estornado = 3,
    Cancelado = 4
}
