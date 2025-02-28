using AutoMapper;
using BliviPedidos.Data;
using BliviPedidos.Models;
using BliviPedidos.Models.ViewModels;
using BliviPedidos.Services.Interfaces;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.EntityFrameworkCore;

namespace BliviPedidos.Services.Implementations
{
    public class ClienteService : BaseService<Cliente>, IClienteService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IPedidoService _pedidoService;

        public ClienteService(ApplicationDbContext context, IMapper mapper, IPedidoService pedidoService) : base(context)
        {
            _context = context;
            _mapper = mapper;
            _pedidoService = pedidoService;
        }

        public async Task<ClienteViewModel> GetClienteByIdAsync(int id)
        {
            var c = await _context.Cliente
                .FirstOrDefaultAsync(c => c.Id == id);

            c.Pedidos = _pedidoService.GetListaPedidos()
                                .Where(p => p.Cadastro.Telefone.Trim() == c.Telefone.Trim())
                                .ToList();

            return _mapper.Map<ClienteViewModel>(c);
        }

        public async Task<List<ClienteViewModel>> GetClientesAsync()
        {
            var listaClientes = await _context.Cliente.ToListAsync();

            foreach (var c in listaClientes)
            {
                c.Pedidos = _pedidoService.GetListaPedidos()
                                .Where(p => p.Cadastro.Telefone.Trim() == c.Telefone.Trim())
                                .ToList();
            }

            return _mapper.Map<List<ClienteViewModel>>(listaClientes);
        }

        public async Task<ClienteViewModel> ProcurarClienteByTelefoneAsync(string telefone)
        {
            var c = await _context.Cliente.FirstOrDefaultAsync(cliente => cliente.Telefone == telefone);

            if (c == null)
                return new ClienteViewModel();

            return _mapper.Map<ClienteViewModel>(c);
        }

        public async Task<ClienteViewModel> RegistrarClienteAsync(Cadastro cadastro)
        {
            var clienteExistente = await ProcurarClienteByTelefoneAsync2(cadastro.Telefone);

            if (clienteExistente != null && clienteExistente.Id > 0)
            {
                clienteExistente.Nome = cadastro.Nome;
                clienteExistente.Email = cadastro.Email;
                clienteExistente.Telefone = cadastro.Telefone;
                clienteExistente.Turma = cadastro.Turma;
                clienteExistente.ResponsavelCerimar = cadastro.ResponsavelCerimar;
                clienteExistente.Endereco = cadastro.Endereco;
                clienteExistente.Complemento = cadastro.Complemento;
                clienteExistente.Bairro = cadastro.Bairro;
                clienteExistente.Municipio = cadastro.Municipio;
                clienteExistente.UF = cadastro.UF;
                clienteExistente.CEP = cadastro.CEP;

                await AtualizarClienteAsync(clienteExistente);

                return _mapper.Map<ClienteViewModel>(clienteExistente);
            }
            else
            {
                var novoCliente = new Cliente
                {
                    Nome = cadastro.Nome,
                    Email = cadastro.Email,
                    ResponsavelCerimar = cadastro.ResponsavelCerimar,
                    Turma = cadastro.Turma,
                    Telefone = cadastro.Telefone,
                    Endereco = cadastro.Endereco,
                    Complemento = cadastro.Complemento,
                    Bairro = cadastro.Bairro,
                    Municipio = cadastro.Municipio,
                    UF = cadastro.UF,
                    CEP = cadastro.CEP
                };

                await InserirClienteAsync(novoCliente);

                return _mapper.Map<ClienteViewModel>(novoCliente);
            }
        }

        private async Task AtualizarClienteAsync(Cliente cliente)
        {
            try
            {
                _context.Cliente.Update(cliente);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Erro ao atualizar o cliente.", ex);
            }
        }

        private async Task InserirClienteAsync(Cliente cliente)
        {
            try
            {
                _context.Cliente.Add(cliente);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Erro ao inserir o cliente.", ex);
            }
        }

        private async Task<Cliente> ProcurarClienteByTelefoneAsync2(string telefone)
        {
            return await _context.Cliente.FirstOrDefaultAsync(c => c.Telefone == telefone);
        }

    }
}
