using System.Security.Claims;
using BeerApi.Application.DTOs;
using BeerApi.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BeerApi.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthService authService) : ControllerBase
{
    private readonly AuthService _authService = authService;

    [HttpPost("register/brewer")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> RegisterBrewer([FromBody] RegisterBrewerDto dto, CancellationToken ct)
    {
        await _authService.RegisterBrewerAsync(dto, ct);
        return Ok(new { message = "Cervejeiro registrado. Confirme seu e-mail antes de fazer login." });
    }

    [HttpPost("register/wholesaler")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> RegisterWholesaler([FromBody] RegisterWholesalerDto dto, CancellationToken ct)
    {
        await _authService.RegisterWholesalerAsync(dto, ct);
        return Ok(new { message = "Atacadista registrado. Confirme seu e-mail antes de fazer login." });
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto, CancellationToken ct)
    {
        var tokens = await _authService.LoginAsync(dto.Email, dto.Password, ct);
        return tokens is null ? Unauthorized() : Ok(tokens);
    }

    [HttpPost("refresh")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto dto, CancellationToken ct)
    {
        var tokens = await _authService.RefreshAsync(dto.RefreshToken, ct);
        return tokens is null ? Unauthorized() : Ok(tokens);
    }

    [HttpPost("logout")]
    [Authorize]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto dto, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        await _authService.LogoutAsync(userId, dto.RefreshToken, ct);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult GetCurrentUser() => Ok(new
    {
        email = User.FindFirstValue(ClaimTypes.Email),
        firstName = User.FindFirstValue(ClaimTypes.GivenName),
        lastName = User.FindFirstValue(ClaimTypes.Surname),
        roles = User.FindAll(ClaimTypes.Role).Select(claim => claim.Value),
        breweryId = User.FindFirst("BreweryId")?.Value,
        wholesalerId = User.FindFirst("WholesalerId")?.Value
    });

    [HttpGet("confirm-email")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string code)
    {
        var confirmed = await _authService.ConfirmEmailAsync(userId, code);
        return confirmed ? Ok(new { message = "E-mail confirmado." }) : BadRequest();
    }

    [HttpPost("resend-confirmation")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResendConfirmation([FromBody] EmailRequestDto dto, CancellationToken ct)
    {
        await _authService.ResendConfirmationAsync(dto.Email, ct);
        return Ok(new { message = "Se a conta estiver pendente, enviaremos um e-mail de confirmação." });
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ForgotPassword([FromBody] EmailRequestDto dto, CancellationToken ct)
    {
        await _authService.ForgotPasswordAsync(dto.Email, ct);
        return Ok(new { message = "Se a conta existir, enviaremos instruções para redefinir a senha." });
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto dto, CancellationToken ct)
    {
        var reset = await _authService.ResetPasswordAsync(dto.Email, dto.Token, dto.NewPassword, ct);
        return reset ? Ok() : BadRequest();
    }
}
