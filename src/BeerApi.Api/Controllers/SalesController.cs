using System.Security.Claims;
using BeerApi.Api.Authorization;
using BeerApi.Application.DTOs;
using BeerApi.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeerApi.Api.Controllers;

[ApiController]
[Route("api/sales")]
[Authorize(Policy = AuthorizationPolicies.BrewerOrAdmin)]
public class SalesController(
    ISaleService saleService,
    IBeerService beerService,
    IAuthorizationService authorizationService) : ControllerBase
{
    private readonly ISaleService _saleService = saleService;
    private readonly IBeerService _beerService = beerService;
    private readonly IAuthorizationService _authorizationService = authorizationService;

    [HttpPost]
    public async Task<IActionResult> CreateSale([FromBody] CreateSaleDto dto, CancellationToken ct)
    {
        var beer = await _beerService.GetByIdAsync(dto.BeerId, ct);
        if (!(await _authorizationService.AuthorizeAsync(User, beer.BreweryId, AuthorizationPolicies.ManageBrewery)).Succeeded)
            return Forbid();

        var sale = await _saleService.CreateSaleAsync(dto, ct);
        return Created($"/api/sales/{sale.Id}", sale);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationQueryDto query, CancellationToken ct)
    {
        int? breweryId = null;
        if (!User.IsInRole("Admin"))
        {
            var claim = User.FindFirstValue("BreweryId");
            if (claim is null || !int.TryParse(claim, out var userBreweryId))
                return Forbid();

            breweryId = userBreweryId;
        }

        return Ok(await _saleService.GetAllAsync(query.Page, query.PageSize, breweryId, ct));
    }
}
