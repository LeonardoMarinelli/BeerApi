using BeerApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BeerApi.Infrastructure;

/// <summary>
/// Used by EF Core CLI tools (dotnet ef migrations) at design time so they don't
/// need a running MySQL instance. The connection string here is never used at runtime.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(
                "Server=localhost;Port=3306;Database=beerapi;User=beerapi_user;Password=beerapi_pass;",
                new MySqlServerVersion(new Version(8, 0, 0)))
            .Options;

        return new AppDbContext(options);
    }
}
