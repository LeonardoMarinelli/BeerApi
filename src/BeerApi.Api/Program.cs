using System.Threading.RateLimiting;
using BeerApi.Api.Middleware;
using BeerApi.Api.HealthChecks;
using BeerApi.Api.Authorization;
using BeerApi.Application.Services;
using BeerApi.Application.Services.Interfaces;
using BeerApi.Domain.Interfaces;
using BeerApi.Infrastructure.Data;
using BeerApi.Infrastructure.Data.Seed;
using BeerApi.Infrastructure.Identity;
using BeerApi.Infrastructure.Repositories;
using BeerApi.Infrastructure.Services;
using BeerApi.Infrastructure.Services.Cached;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;

{
    var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (dir != null && !File.Exists(Path.Combine(dir.FullName, ".env")))
        dir = dir.Parent;
    if (dir != null)
        Env.Load(Path.Combine(dir.FullName, ".env"));
}

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 1_048_576);
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [])
            .AllowAnyHeader()
            .AllowAnyMethod()));

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BeerApi",
        Version = "v1",
        Description = "Belgian brewery & wholesaler management API"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        Description = "Paste the bearer access token obtained from POST /api/auth/login"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var connectionString =
    $"Server={Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost"};" +
    $"Port={Environment.GetEnvironmentVariable("DB_PORT") ?? "3306"};" +
    $"Database={Environment.GetEnvironmentVariable("MYSQL_DATABASE") ?? "beerapi"};" +
    $"User={Environment.GetEnvironmentVariable("MYSQL_USER") ?? "beerapi_user"};" +
    $"Password={Environment.GetEnvironmentVariable("MYSQL_PASSWORD") ?? "beerapi_pass"};";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.Parse("8.0.0-mysql"),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure()));

var redisConnectionString = builder.Configuration["REDIS_CONNECTION"]
    ?? builder.Configuration["Redis:ConnectionString"]
    ?? $"localhost:{builder.Configuration["REDIS_PORT"] ?? "6379"}";

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = "BeerApi:";
});

builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromSeconds(builder.Configuration.GetValue("Cache:ExpirationSeconds", 300)),
        LocalCacheExpiration = TimeSpan.FromSeconds(builder.Configuration.GetValue("Cache:LocalExpirationSeconds", 30))
    };
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>()
    .AddCheck<RedisCacheHealthCheck>("redis");

builder.Services.AddIdentityApiEndpoints<ApplicationUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddClaimsPrincipalFactory<ApplicationUserClaimsPrincipalFactory>();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.SignIn.RequireConfirmedEmail = true;
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
});
builder.Services.Configure<BearerTokenOptions>(IdentityConstants.BearerScheme, options =>
    options.BearerTokenExpiration = TimeSpan.FromMinutes(builder.Configuration.GetValue("Auth:AccessTokenMinutes", 15)));

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.AdminOnly, policy => policy.RequireRole("Admin"));
    options.AddPolicy(AuthorizationPolicies.BrewerOrAdmin, policy => policy.RequireRole("Brewer", "Admin"));
    options.AddPolicy(AuthorizationPolicies.ManageBrewery, policy =>
    {
        policy.RequireRole("Brewer", "Admin");
        policy.AddRequirements(new BreweryOwnerRequirement());
    });
});
builder.Services.AddSingleton<IAuthorizationHandler, BreweryOwnerAuthorizationHandler>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IBreweryRepository, BreweryRepository>();
builder.Services.AddScoped<IBeerRepository, BeerRepository>();
builder.Services.AddScoped<IWholesalerRepository, WholesalerRepository>();
builder.Services.AddScoped<ISaleRepository, SaleRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<BreweryService>();
builder.Services.AddScoped<BeerService>();
builder.Services.AddScoped<WholesalerService>();
builder.Services.AddScoped<SaleService>();
if (builder.Configuration.GetValue("Cache:Enabled", true))
{
    builder.Services.AddScoped<IBreweryService, CachedBreweryService>();
    builder.Services.AddScoped<IBeerService, CachedBeerService>();
    builder.Services.AddScoped<IWholesalerService, CachedWholesalerService>();
    builder.Services.AddScoped<ISaleService, CachedSaleService>();
}
else
{
    builder.Services.AddScoped<IBreweryService>(services => services.GetRequiredService<BreweryService>());
    builder.Services.AddScoped<IBeerService>(services => services.GetRequiredService<BeerService>());
    builder.Services.AddScoped<IWholesalerService>(services => services.GetRequiredService<WholesalerService>());
    builder.Services.AddScoped<ISaleService>(services => services.GetRequiredService<SaleService>());
}
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AuthTokenService>();
builder.Services.AddScoped<IMailSender, SmtpMailSender>();
builder.Services.AddHostedService<RefreshTokenCleanupService>();
builder.Services.AddScoped<AuditLogService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DataSeeder.SeedRolesAndAdminAsync(scope.ServiceProvider);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging(options =>
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} → {StatusCode} em {Elapsed:0.0}ms");
app.UseMiddleware<ExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

app.MapControllers();

try
{
    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "A aplicação encerrou inesperadamente");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
