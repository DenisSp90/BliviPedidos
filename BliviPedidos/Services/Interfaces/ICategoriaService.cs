
using BliviPedidos.Models;

namespace BliviPedidos.Services.Interfaces;

public interface ICategoriaService 
{
    Task<List<Categoria>> GetCategoriaListAsync();
    Task<bool> RegistrarCategoriaAsync(Categoria categoria);


}
