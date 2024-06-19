using AutoMapper;
using BliviPedidos.Models.ViewModels;
using BliviPedidos.Models;

namespace BliviPedidos.Profiles
{
    public class ProdutoProfile : Profile
    {
        public ProdutoProfile()
        {
            CreateMap<Produto, ProdutoViewModel>();
            CreateMap<ProdutoViewModel, Produto>();
        }
    }
}
