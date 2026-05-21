using System.Security.Claims;
using System.Text.Json;
using BeerApi.Domain.Entities;
using BeerApi.Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace BeerApi.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        IHttpContextAccessor? httpContextAccessor = null)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public DbSet<Brewery> Breweries => Set<Brewery>();
    public DbSet<Beer> Beers => Set<Beer>();
    public DbSet<Wholesaler> Wholesalers => Set<Wholesaler>();
    public DbSet<WholesalerBeer> WholesalerBeers => Set<WholesalerBeer>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var auditEntries = BuildAuditEntries();
        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var entry in auditEntries.Where(e => e.EntityId == string.Empty))
            entry.EntityId = GetEntityId(ChangeTracker.Entries()
                .First(e => ReferenceEquals(e.Entity, entry)));

        if (auditEntries.Count > 0)
        {
            AuditLogs.AddRange(auditEntries);
            await base.SaveChangesAsync(cancellationToken);
        }

        return result;
    }


    private List<AuditLog> BuildAuditEntries()
    {
        ChangeTracker.DetectChanges();

        var userId = _httpContextAccessor?.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userEmail = _httpContextAccessor?.HttpContext?.User.FindFirstValue(ClaimTypes.Email);

        var auditLogs = new List<AuditLog>();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLog) continue;
            if (entry.State is EntityState.Detached or EntityState.Unchanged) continue;

            var action = entry.State switch
            {
                EntityState.Added    => "Create",
                EntityState.Modified => "Update",
                EntityState.Deleted  => "Delete",
                _                    => null
            };

            if (action is null) continue;

            var log = new AuditLog
            {
                EntityName = entry.Metadata.ClrType.Name,
                EntityId   = GetEntityId(entry),
                Action     = action,
                OldValues  = action != "Create" ? SerializeValues(entry.OriginalValues) : null,
                NewValues  = action != "Delete" ? SerializeValues(entry.CurrentValues)  : null,
                Timestamp  = DateTime.UtcNow,
                UserId     = userId,
                UserEmail  = userEmail
            };

            if (action == "Create" && log.EntityId == "0")
                log.EntityId = string.Empty;

            auditLogs.Add(log);
        }

        return auditLogs;
    }

    private static string GetEntityId(EntityEntry entry)
    {
        var keyValues = entry.Metadata.FindPrimaryKey()
            ?.Properties
            .Select(p => entry.Property(p.Name).CurrentValue?.ToString() ?? "null");

        return keyValues is not null ? string.Join(",", keyValues) : "unknown";
    }

    private static string SerializeValues(PropertyValues values)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var prop in values.Properties)
            dict[prop.Name] = values[prop];

        return JsonSerializer.Serialize(dict);
    }
}
