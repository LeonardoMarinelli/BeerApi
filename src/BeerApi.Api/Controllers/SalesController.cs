using System.Security.Claims;
using BeerApi.Application.DTOs;
using BeerApi.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeerApi.Api.Controllers;

[ApiController]
[Route("api/sales")]
[Authorize(Roles = "Brewer,Admin")]
public class SalesController(ISaleService saleService, IBeerService beerService) : ControllerBase
{
    private readonly ISaleService _saleService = saleService;
    private readonly IBeerService _beerService = beerService;

    [HttpPost]
    public async Task<IActionResult> CreateSale([FromBody] CreateSaleDto dto, CancellationToken ct)
    {
        if (!User.IsInRole("Admin"))
        {
            var beer = await _beerService.GetByIdAsync(dto.BeerId, ct);
            var userBreweryIdClaim = User.FindFirstValue("BreweryId");

            if (userBreweryIdClaim is null ||
                !int.TryParse(userBreweryIdClaim, out var userBreweryId) ||
                beer.BreweryId != userBreweryId)
            {
                return Forbid();
            }
        }

        var sale = await _saleService.CreateSaleAsync(dto, ct);
        return Created($"/api/sales/{sale.Id}", sale);
    }
}
