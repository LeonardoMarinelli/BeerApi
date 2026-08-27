using BeerApi.Application.DTOs;
using BeerApi.Application.Services.Interfaces;
using BeerApi.Domain.Interfaces;

namespace BeerApi.Application.Services;

public class AuditLogService(IAuditLogRepository repository) : IAuditLogService
{
    private readonly IAuditLogRepository _repository = repository;

    public async Task<PagedResultDto<AuditLogDto>> GetAllAsync(
        int page, int pageSize, string? entityName, CancellationToken ct = default)
    {
        var (logs, totalCount) = await _repository.GetAllAsync(page, pageSize, entityName, ct);
        var items = logs.Select(a => new AuditLogDto(
            a.Id, a.EntityName, a.EntityId, a.Action,
            a.OldValues, a.NewValues, a.Timestamp, a.UserId, a.UserEmail));
        return new PagedResultDto<AuditLogDto>(items, page, pageSize, totalCount);
    }
}
