using System.Security.Cryptography;
using System.Text;
using BeerApi.Application.DTOs;
using BeerApi.Domain.Entities;
using BeerApi.Infrastructure.Data;
using BeerApi.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace BeerApi.Infrastructure.Services;

public sealed class AuthTokenService(
    AppDbContext context,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IOptionsMonitor<BearerTokenOptions> bearerOptions,
    IConfiguration configuration)
{
    public async Task<AuthTokenResponseDto?> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
            return null;

        var result = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        return result.Succeeded
            ? await IssueAsync(user, Guid.NewGuid().ToString("N"), ct)
            : null;
    }

    public async Task<AuthTokenResponseDto?> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        AuthTokenResponseDto? response = null;
        var tokenHash = HashToken(refreshToken);

        await context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            response = null;
            await using var transaction = await context.Database.BeginTransactionAsync(ct);
            try
            {
                var current = await context.RefreshTokens.AsNoTracking()
                    .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, ct);

                if (current is null)
                {
                    await transaction.CommitAsync(ct);
                    return;
                }

                if (current.ConsumedAt is not null || current.RevokedAt is not null)
                {
                    await RevokeFamilyAsync(current.FamilyId, DateTime.UtcNow, ct);
                    await transaction.CommitAsync(ct);
                    return;
                }

                var now = DateTime.UtcNow;
                if (current.ExpiresAt <= now)
                {
                    await transaction.CommitAsync(ct);
                    return;
                }

                var consumed = await context.RefreshTokens
                    .Where(token => token.Id == current.Id &&
                                    token.ConsumedAt == null &&
                                    token.RevokedAt == null &&
                                    token.ExpiresAt > now)
                    .ExecuteUpdateAsync(
                        update => update.SetProperty(token => token.ConsumedAt, now),
                        ct);

                if (consumed != 1)
                {
                    await RevokeFamilyAsync(current.FamilyId, now, ct);
                    await transaction.CommitAsync(ct);
                    return;
                }

                var user = await userManager.FindByIdAsync(current.UserId);
                if (user is null)
                {
                    await RevokeFamilyAsync(current.FamilyId, now, ct);
                    await transaction.CommitAsync(ct);
                    return;
                }

                response = await IssueAsync(user, current.FamilyId, ct);
                await transaction.CommitAsync(ct);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        });

        return response;
    }

    public async Task RevokeFamilyForUserAsync(string userId, string refreshToken, CancellationToken ct = default)
    {
        var tokenHash = HashToken(refreshToken);
        var token = await context.RefreshTokens.AsNoTracking()
            .SingleOrDefaultAsync(entry => entry.UserId == userId && entry.TokenHash == tokenHash, ct);
        if (token is not null)
            await RevokeFamilyAsync(token.FamilyId, DateTime.UtcNow, ct);
    }

    public Task RevokeAllForUserAsync(string userId, CancellationToken ct = default) =>
        context.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ExecuteUpdateAsync(
                update => update.SetProperty(token => token.RevokedAt, DateTime.UtcNow),
                ct);

    private async Task<AuthTokenResponseDto> IssueAsync(ApplicationUser user, string familyId, CancellationToken ct)
    {
        var options = bearerOptions.Get(IdentityConstants.BearerScheme);
        var now = DateTimeOffset.UtcNow;
        var principal = await signInManager.CreateUserPrincipalAsync(user);
        var properties = new AuthenticationProperties
        {
            IssuedUtc = now,
            ExpiresUtc = now.Add(options.BearerTokenExpiration)
        };
        var ticket = new AuthenticationTicket(
            principal,
            properties,
            $"{IdentityConstants.BearerScheme}:AccessToken");
        var accessToken = options.BearerTokenProtector.Protect(ticket);
        var refreshToken = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var createdAt = DateTime.UtcNow;

        context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            FamilyId = familyId,
            TokenHash = HashToken(refreshToken),
            CreatedAt = createdAt,
            ExpiresAt = createdAt.AddDays(configuration.GetValue("Auth:RefreshTokenDays", 14))
        });
        await context.SaveChangesAsync(ct);

        return new AuthTokenResponseDto(
            "Bearer",
            accessToken,
            (int)options.BearerTokenExpiration.TotalSeconds,
            refreshToken);
    }

    private Task<int> RevokeFamilyAsync(string familyId, DateTime revokedAt, CancellationToken ct) =>
        context.RefreshTokens
            .Where(token => token.FamilyId == familyId && token.RevokedAt == null)
            .ExecuteUpdateAsync(
                update => update.SetProperty(token => token.RevokedAt, revokedAt),
                ct);

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}