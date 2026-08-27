using BeerApi.Domain.Entities;
using BeerApi.Domain.Interfaces;
using BeerApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BeerApi.Infrastructure.Repositories;

public class AuditLogRepository(AppDbContext context) : IAuditLogRepository
{
    private readonly AppDbContext _context = context;

    public async Task<(IEnumerable<AuditLog> Items, int TotalCount)> GetAllAsync(
        int page, int pageSize, string? entityName, CancellationToken ct = default)
    {
        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityName))
            query = query.Where(a => a.EntityName == entityName);

        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, totalCount);
    }
}
