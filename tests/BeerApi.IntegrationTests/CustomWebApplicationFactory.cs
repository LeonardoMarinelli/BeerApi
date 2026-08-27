using System.Threading.RateLimiting;
using BeerApi.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Testcontainers.MySql;

namespace BeerApi.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminEmail = "admin@beerapi.test";
    public const string AdminPassword = "AdminTest@123!";

    private readonly MySqlContainer _mysqlContainer = new MySqlBuilder("mysql:8.0")
        .WithDatabase("beerapi_test")
        .WithUsername("beerapi_test")
        .WithPassword("beerapi_test")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AdminUser:Email"] = AdminEmail,
                ["AdminUser:Password"] = AdminPassword
            }));

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options =>
                options.UseMySql(
                    _mysqlContainer.GetConnectionString(),
                    ServerVersion.Parse("8.0.0-mysql"),
                    mySqlOptions => mySqlOptions.EnableRetryOnFailure()));

            services.RemoveAll<IConfigureOptions<RateLimiterOptions>>();
            services.Configure<RateLimiterOptions>(options =>
            {
                options.AddPolicy("auth", _ => RateLimitPartition.GetNoLimiter("auth-unlimited"));
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            });
        });
    }

    public Task InitializeAsync() => _mysqlContainer.StartAsync();

    async Task IAsyncLifetime.DisposeAsync() => await _mysqlContainer.DisposeAsync();
}
