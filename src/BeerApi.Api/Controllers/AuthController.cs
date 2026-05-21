using BeerApi.Application.DTOs;
using BeerApi.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BeerApi.Api.Controllers;

/// <summary>
/// Custom registration endpoints for Brewers and Wholesalers.
/// Login, refresh, and manage endpoints are provided by the built-in Identity API
/// at /api/auth/login, /api/auth/refresh, /api/auth/manage/*, etc.
/// </summary>
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController(IAuthService authService) : ControllerBase
{
    private readonly IAuthService _authService = authService;

    [HttpPost("register/brewer")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> RegisterBrewer([FromBody] RegisterBrewerDto dto, CancellationToken ct)
    {
        await _authService.RegisterBrewerAsync(dto, ct);
        return Ok(new { message = "Cervejeiro registrado com sucesso. Você já pode fazer login em POST /api/auth/login." });
    }

    [HttpPost("register/wholesaler")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> RegisterWholesaler([FromBody] RegisterWholesalerDto dto, CancellationToken ct)
    {
        await _authService.RegisterWholesalerAsync(dto, ct);
        return Ok(new { message = "Atacadista registrado com sucesso. Você já pode fazer login em POST /api/auth/login." });
    }
}
