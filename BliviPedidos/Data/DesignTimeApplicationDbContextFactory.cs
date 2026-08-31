using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BliviPedidos.Data;

public sealed class DesignTimeApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseMySql(
                "Server=localhost;Database=blivipedidos_design;User=design;Password=design;",
                new MySqlServerVersion(new Version(8, 0, 0)))
            .Options;

        return new ApplicationDbContext(options);
    }
}
