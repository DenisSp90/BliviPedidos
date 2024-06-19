using BliviPedidos.Models;

namespace BliviPedidos.Services.Interfaces
{
    public interface ICadastroService
    {
        Cadastro Update(int cadastroId, Cadastro novocadastro);
        Cadastro GetCadastro(int cadastroId);
    }
}
