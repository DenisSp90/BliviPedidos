using AutoMapper;
using BliviPedidos.Models.ViewModels;
using BliviPedidos.Models;

namespace BliviPedidos.Profiles;

public class CategoriaProfile : Profile
{
    public CategoriaProfile()
    {
        CreateMap<Categoria, CategoriaViewModel>();
        CreateMap<CategoriaViewModel, Categoria>();
    }
}
