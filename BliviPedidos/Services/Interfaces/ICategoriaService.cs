
using BliviPedidos.Models;

namespace BliviPedidos.Services.Interfaces;

public interface ICategoriaService 
{
    Task<List<Categoria>> GetCategoriaListAsync();
    Task<Categoria?> ProcurarCategoriaAsync(int id);
    Task<bool> RegistrarCategoriaAsync(Categoria categoria);
}
