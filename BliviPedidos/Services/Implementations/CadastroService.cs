using BliviPedidos.Data;
using BliviPedidos.Models;
using BliviPedidos.Services.Interfaces;

namespace BliviPedidos.Services.Implementations
{
    public class CadastroService : BaseService<Cadastro>, ICadastroService
    {
        public CadastroService(ApplicationDbContext context) : base(context)
        {
        }

        public Cadastro GetCadastro(int cadastroId)
        {
            return dbSet
                .Where(c => c.Id == cadastroId)
                .FirstOrDefault();
        }

        public Cadastro Update(int cadastroId, Cadastro novocadastro)
        {
            var cadastroDB = dbSet.Where(c => c.Id == cadastroId)
                .FirstOrDefault();

            if (cadastroDB == null)
                throw new ArgumentNullException("cadastro");

            cadastroDB.Update(novocadastro);
            _context.SaveChanges();

            return cadastroDB;
        }
    }
}
