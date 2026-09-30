using System.Text;
using BeerApi.Application.DTOs;
using BeerApi.Domain.Entities;
using BeerApi.Domain.Exceptions;
using BeerApi.Infrastructure.Data;
using BeerApi.Infrastructure.Identity;
using BeerApi.Infrastructure.Services.Cached;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

namespace BeerApi.Infrastructure.Services;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    AppDbContext context,
    HybridCache cache,
    AuthTokenService tokenService,
    IMailSender mailSender,
    IConfiguration configuration)
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly AppDbContext _context = context;
    private readonly HybridCache _cache = cache;
    private readonly AuthTokenService _tokenService = tokenService;
    private readonly IMailSender _mailSender = mailSender;
    private readonly IConfiguration _configuration = configuration;

    public async Task RegisterBrewerAsync(RegisterBrewerDto dto, CancellationToken ct = default)
    {
        ApplicationUser? registeredUser = null;
        await _context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                var brewery = new Brewery
                {
                    Name = dto.BreweryName,
                    Country = dto.BreweryCountry,
                    Description = dto.BreweryDescription
                };
                _context.Breweries.Add(brewery);
                await _context.SaveChangesAsync(ct);

                var user = new ApplicationUser
                {
                    UserName = dto.Email,
                    Email = dto.Email,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    EmailConfirmed = false,
                    BreweryId = brewery.Id
                };
                registeredUser = user;

                var result = await _userManager.CreateAsync(user, dto.Password);
                if (!result.Succeeded)
                    throw new BusinessException(string.Join("; ", result.Errors.Select(e => e.Description)));

                await _userManager.AddToRoleAsync(user, "Brewer");
                await transaction.CommitAsync(ct);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        });
        await _cache.RemoveByTagAsync(CacheTags.Breweries, ct);
        await SendConfirmationEmailAsync(registeredUser!, ct);
    }

    public async Task RegisterWholesalerAsync(RegisterWholesalerDto dto, CancellationToken ct = default)
    {
        ApplicationUser? registeredUser = null;
        await _context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                var wholesaler = new Wholesaler
                {
                    Name = dto.WholesalerName,
                    Address = dto.WholesalerAddress
                };
                _context.Wholesalers.Add(wholesaler);
                await _context.SaveChangesAsync(ct);

                var user = new ApplicationUser
                {
                    UserName = dto.Email,
                    Email = dto.Email,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    EmailConfirmed = false,
                    WholesalerId = wholesaler.Id
                };
                registeredUser = user;

                var result = await _userManager.CreateAsync(user, dto.Password);
                if (!result.Succeeded)
                    throw new BusinessException(string.Join("; ", result.Errors.Select(e => e.Description)));

                await _userManager.AddToRoleAsync(user, "Wholesaler");
                await transaction.CommitAsync(ct);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        });
        await _cache.RemoveByTagAsync(CacheTags.Wholesalers, ct);
        await SendConfirmationEmailAsync(registeredUser!, ct);
    }

    public Task<AuthTokenResponseDto?> LoginAsync(string email, string password, CancellationToken ct = default) =>
        _tokenService.LoginAsync(email, password, ct);

    public Task<AuthTokenResponseDto?> RefreshAsync(string refreshToken, CancellationToken ct = default) =>
        _tokenService.RefreshAsync(refreshToken, ct);

    public Task LogoutAsync(string userId, string refreshToken, CancellationToken ct = default) =>
        _tokenService.RevokeFamilyForUserAsync(userId, refreshToken, ct);

    public async Task<bool> ConfirmEmailAsync(string userId, string code)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null || !TryDecodeToken(code, out var token))
            return false;

        var result = await _userManager.ConfirmEmailAsync(user, token);
        return result.Succeeded;
    }

    public async Task ResendConfirmationAsync(string email, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is not null && !user.EmailConfirmed)
            await SendConfirmationEmailAsync(user, ct);
    }

    public async Task ForgotPasswordAsync(string email, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null || !user.EmailConfirmed)
            return;

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = EncodeToken(token);
        var resetEndpoint = $"{GetPublicBaseUrl()}/api/auth/reset-password";
        await _mailSender.SendAsync(
            email,
            "Redefinição de senha BeerApi",
            $"Envie uma requisição POST para {resetEndpoint} com seu e-mail, este token e a nova senha:\n{encodedToken}",
            ct);
    }

    public async Task<bool> ResetPasswordAsync(string email, string encodedToken, string newPassword, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null || !TryDecodeToken(encodedToken, out var token))
            return false;

        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
            return false;

        await _tokenService.RevokeAllForUserAsync(user.Id, ct);
        return true;
    }

    private async Task SendConfirmationEmailAsync(ApplicationUser user, CancellationToken ct)
    {
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmationUrl = $"{GetPublicBaseUrl()}/api/auth/confirm-email" +
                              $"?userId={Uri.EscapeDataString(user.Id)}&code={Uri.EscapeDataString(EncodeToken(token))}";
        await _mailSender.SendAsync(
            user.Email!,
            "Confirme seu e-mail BeerApi",
            $"Confirme sua conta acessando o link:\n{confirmationUrl}",
            ct);
    }

    private string GetPublicBaseUrl() =>
        (_configuration["App:PublicBaseUrl"] ?? "http://localhost:5157").TrimEnd('/');

    private static string EncodeToken(string token) =>
        WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

    private static bool TryDecodeToken(string encodedToken, out string token)
    {
        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedToken));
            return true;
        }
        catch (FormatException)
        {
            token = string.Empty;
            return false;
        }
    }
}
