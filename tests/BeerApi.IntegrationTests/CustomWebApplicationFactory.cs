using System.Threading.RateLimiting;
using BeerApi.Infrastructure.Services;
using BeerApi.IntegrationTests.Helpers;
using BeerApi.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Testcontainers.MySql;
using Testcontainers.Redis;

namespace BeerApi.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminEmail = "admin@beerapi.test";
    public const string AdminPassword = "AdminTest@123!";
    public CapturingEmailSender EmailSender { get; } = new();

    private readonly MySqlContainer _mysqlContainer = new MySqlBuilder("mysql:8.0")
        .WithDatabase("beerapi_test")
        .WithUsername("beerapi_test")
        .WithPassword("beerapi_test")
        .Build();
    private readonly RedisContainer _redisContainer = new RedisBuilder("redis:7-alpine").Build();

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
            services.RemoveAll<IMailSender>();
            services.AddSingleton<IMailSender>(EmailSender);

            services.RemoveAll<IDistributedCache>();
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = _redisContainer.GetConnectionString();
                options.InstanceName = "BeerApi:";
            });

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

    public Task InitializeAsync() => Task.WhenAll(_mysqlContainer.StartAsync(), _redisContainer.StartAsync());

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _redisContainer.DisposeAsync();
        await _mysqlContainer.DisposeAsync();
    }
}
