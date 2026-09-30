using BeerApi.Application.DTOs;
using BeerApi.Application.Services.Interfaces;
using BeerApi.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeerApi.Api.Controllers;

[ApiController]
[Route("api/breweries")]
[Authorize]
public class BreweriesController(
    IBreweryService breweryService,
    IBeerService beerService,
    IAuthorizationService authorizationService) : ControllerBase
{
    private readonly IBreweryService _breweryService = breweryService;
    private readonly IBeerService _beerService = beerService;
    private readonly IAuthorizationService _authorizationService = authorizationService;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationQueryDto query, CancellationToken ct) =>
        Ok(await _breweryService.GetAllAsync(query.Page, query.PageSize, ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct) =>
        Ok(await _breweryService.GetByIdAsync(id, ct));

    [HttpGet("{breweryId:int}/beers")]
    public async Task<IActionResult> GetBeers(int breweryId, CancellationToken ct) =>
        Ok(await _beerService.GetByBreweryIdAsync(breweryId, ct));

    [HttpPost("{breweryId:int}/beers")]
    [Authorize(Policy = AuthorizationPolicies.BrewerOrAdmin)]
    public async Task<IActionResult> CreateBeer(int breweryId, [FromBody] CreateBeerDto dto, CancellationToken ct)
    {
        if (!(await _authorizationService.AuthorizeAsync(User, breweryId, AuthorizationPolicies.ManageBrewery)).Succeeded)
            return Forbid();

        var beer = await _beerService.CreateAsync(breweryId, dto, ct);
        return CreatedAtAction(nameof(GetBeers), new { breweryId }, beer);
    }

    [HttpPut("{breweryId:int}/beers/{beerId:int}")]
    [Authorize(Policy = AuthorizationPolicies.BrewerOrAdmin)]
    public async Task<IActionResult> UpdateBeer(int breweryId, int beerId, [FromBody] UpdateBeerDto dto, CancellationToken ct)
    {
        if (!(await _authorizationService.AuthorizeAsync(User, breweryId, AuthorizationPolicies.ManageBrewery)).Succeeded)
            return Forbid();

        var beer = await _beerService.UpdateAsync(breweryId, beerId, dto, ct);
        return Ok(beer);
    }

    [HttpDelete("{breweryId:int}/beers/{beerId:int}")]
    [Authorize(Policy = AuthorizationPolicies.BrewerOrAdmin)]
    public async Task<IActionResult> DeleteBeer(int breweryId, int beerId, CancellationToken ct)
    {
        if (!(await _authorizationService.AuthorizeAsync(User, breweryId, AuthorizationPolicies.ManageBrewery)).Succeeded)
            return Forbid();

        await _beerService.DeleteAsync(breweryId, beerId, ct);
        return NoContent();
    }

}
