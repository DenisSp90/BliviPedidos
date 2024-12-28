using BliviPedidos.Data;
using BliviPedidos.Models;
using Microsoft.EntityFrameworkCore;

namespace BliviPedidos.Services;

public class BaseService<T> where T : BaseModel
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> dbSet;

    public BaseService(ApplicationDbContext context)
    {
        _context = context;
        this.dbSet = context.Set<T>();
    }
}
