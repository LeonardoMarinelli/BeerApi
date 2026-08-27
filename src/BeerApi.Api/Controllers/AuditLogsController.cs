using BeerApi.Application.DTOs;
using BeerApi.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeerApi.Api.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Roles = "Admin")]
public class AuditLogsController(IAuditLogService auditLogService) : ControllerBase
{
    private readonly IAuditLogService _auditLogService = auditLogService;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaginationQueryDto query, [FromQuery] string? entityName, CancellationToken ct) =>
        Ok(await _auditLogService.GetAllAsync(query.Page, query.PageSize, entityName, ct));
}
