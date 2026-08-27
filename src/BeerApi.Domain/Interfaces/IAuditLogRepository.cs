using BeerApi.Domain.Entities;

namespace BeerApi.Domain.Interfaces;

public interface IAuditLogRepository
{
    Task<(IEnumerable<AuditLog> Items, int TotalCount)> GetAllAsync(
        int page, int pageSize, string? entityName, CancellationToken ct = default);
}
