using BliviPedidos.Models;

namespace BliviPedidos.Services.Interfaces;

public interface IDadosConsumidorCheckoutService
{
    DadosConsumidorCheckout? Obter(int lojaId);
    void Salvar(int lojaId, DadosConsumidorCheckout dados);
    void Limpar(int lojaId);
}
