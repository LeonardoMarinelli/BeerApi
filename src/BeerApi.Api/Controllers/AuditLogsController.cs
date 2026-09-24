using BeerApi.Application.DTOs;
using BeerApi.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeerApi.Api.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Roles = "Admin")]
public class AuditLogsController(AuditLogService auditLogService) : ControllerBase
{
    private readonly AuditLogService _auditLogService = auditLogService;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaginationQueryDto query, [FromQuery] string? entityName, CancellationToken ct) =>
        Ok(await _auditLogService.GetAllAsync(query.Page, query.PageSize, entityName, ct));
}
