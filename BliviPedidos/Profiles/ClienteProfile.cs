using AutoMapper;
using BliviPedidos.Models.ViewModels;
using BliviPedidos.Models;

namespace BliviPedidos.Profiles
{
    public class ClienteProfile : Profile
    {
        public ClienteProfile()
        {
            CreateMap<Cliente, ClienteViewModel>();
            CreateMap<ClienteViewModel, Cliente>();
        }
    }
}
