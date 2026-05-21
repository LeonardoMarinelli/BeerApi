using BeerApi.Application.DTOs;
using BeerApi.Application.Services.Interfaces;
using BeerApi.Domain.Entities;
using BeerApi.Domain.Exceptions;
using BeerApi.Infrastructure.Data;
using BeerApi.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace BeerApi.Infrastructure.Services;

public class AuthService(UserManager<ApplicationUser> userManager, AppDbContext context) : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly AppDbContext _context = context;

    public async Task RegisterBrewerAsync(RegisterBrewerDto dto)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var brewery = new Brewery
            {
                Name = dto.BreweryName,
                Country = dto.BreweryCountry,
                Description = dto.BreweryDescription
            };
            _context.Breweries.Add(brewery);
            await _context.SaveChangesAsync();

            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                EmailConfirmed = true,
                BreweryId = brewery.Id
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                throw new BusinessException(string.Join("; ", result.Errors.Select(e => e.Description)));

            await _userManager.AddToRoleAsync(user, "Brewer");
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task RegisterWholesalerAsync(RegisterWholesalerDto dto)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var wholesaler = new Wholesaler
            {
                Name = dto.WholesalerName,
                Address = dto.WholesalerAddress
            };
            _context.Wholesalers.Add(wholesaler);
            await _context.SaveChangesAsync();

            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                EmailConfirmed = true,
                WholesalerId = wholesaler.Id
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                throw new BusinessException(string.Join("; ", result.Errors.Select(e => e.Description)));

            await _userManager.AddToRoleAsync(user, "Wholesaler");
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
