using BliviPedidos.Models;
using BliviPedidos.Models.ViewModels;

namespace BliviPedidos.Services.Interfaces;

public interface IClienteService
{
    Task<List<ClienteViewModel>> GetClientesAsync();
    Task<ClienteViewModel> GetClienteByIdAsync(int id);
    Task<ClienteViewModel> ProcurarClienteByTelefoneAsync(string telefone);
    Task<ClienteViewModel> RegistrarClienteAsync(Cadastro cadastro);
}
