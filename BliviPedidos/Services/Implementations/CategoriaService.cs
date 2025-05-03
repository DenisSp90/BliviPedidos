using BliviPedidos.Data;
using BliviPedidos.Models;
using BliviPedidos.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BliviPedidos.Services.Implementations;

public class CategoriaService : ICategoriaService
{
    private readonly ApplicationDbContext _context;

    public CategoriaService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Categoria>> GetCategoriaListAsync()
    {
        return await _context.Categoria.ToListAsync();
    }

    public async Task<bool> RegistrarCategoriaAsync(Categoria categoria)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(categoria.Nome))
                throw new ArgumentException("O nome da categoria é obrigatório.");

            if (categoria.Id > 0)
            {
                // Buscar a categoria existente no banco de dados
                var categoriaAtual = await _context.Categoria
                    .Include(c => c.Produtos) // Inclui os produtos relacionados
                    .FirstOrDefaultAsync(p => p.Id == categoria.Id);

                if (categoriaAtual != null)
                {
                    // Atualizar os dados da categoria
                    categoriaAtual.Nome = categoria.Nome;
                    categoriaAtual.IsAtivo = categoria.IsAtivo;

                    // Atualizar o estado dos produtos vinculados
                    if (categoriaAtual.Produtos != null && categoriaAtual.Produtos.Any())
                    {
                        foreach (var produto in categoriaAtual.Produtos)
                        {
                            produto.IsAtivo = categoria.IsAtivo;
                            _context.Produto.Update(produto);
                        }
                    }

                    _context.Categoria.Update(categoriaAtual);
                }
                else
                {
                    throw new KeyNotFoundException("Categoria não encontrada para atualização.");
                }
            }
            else
            {
                // Adicionar nova categoria
                _context.Categoria.Add(categoria);
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Erro ao registrar a categoria.", ex);
        }
    }


}
