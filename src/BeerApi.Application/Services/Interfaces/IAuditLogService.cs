using BeerApi.Application.DTOs;

namespace BeerApi.Application.Services.Interfaces;

public interface IAuditLogService
{
    Task<PagedResultDto<AuditLogDto>> GetAllAsync(
        int page, int pageSize, string? entityName, CancellationToken ct = default);
}
