using System.Security.Claims;
using BeerApi.Application.DTOs;
using BeerApi.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeerApi.Api.Controllers;

[ApiController]
[Route("api/breweries")]
[Authorize]
public class BreweriesController(IBreweryService breweryService, IBeerService beerService) : ControllerBase
{
    private readonly IBreweryService _breweryService = breweryService;
    private readonly IBeerService _beerService = beerService;

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _breweryService.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        Ok(await _breweryService.GetByIdAsync(id));

    [HttpGet("{breweryId:int}/beers")]
    public async Task<IActionResult> GetBeers(int breweryId) =>
        Ok(await _beerService.GetByBreweryIdAsync(breweryId));

    [HttpPost("{breweryId:int}/beers")]
    [Authorize(Roles = "Brewer,Admin")]
    public async Task<IActionResult> CreateBeer(int breweryId, [FromBody] CreateBeerDto dto)
    {
        if (!IsBreweryOwnerOrAdmin(breweryId))
            return Forbid();

        var beer = await _beerService.CreateAsync(breweryId, dto);
        return CreatedAtAction(nameof(GetBeers), new { breweryId }, beer);
    }

    [HttpPut("{breweryId:int}/beers/{beerId:int}")]
    [Authorize(Roles = "Brewer,Admin")]
    public async Task<IActionResult> UpdateBeer(int breweryId, int beerId, [FromBody] UpdateBeerDto dto)
    {
        if (!IsBreweryOwnerOrAdmin(breweryId))
            return Forbid();

        var beer = await _beerService.UpdateAsync(breweryId, beerId, dto);
        return Ok(beer);
    }

    [HttpDelete("{breweryId:int}/beers/{beerId:int}")]
    [Authorize(Roles = "Brewer,Admin")]
    public async Task<IActionResult> DeleteBeer(int breweryId, int beerId)
    {
        if (!IsBreweryOwnerOrAdmin(breweryId))
            return Forbid();

        await _beerService.DeleteAsync(breweryId, beerId);
        return NoContent();
    }

    private bool IsBreweryOwnerOrAdmin(int breweryId)
    {
        if (User.IsInRole("Admin")) return true;

        var claim = User.FindFirstValue("BreweryId");
        return claim is not null && int.TryParse(claim, out var userBreweryId) && userBreweryId == breweryId;
    }
}
