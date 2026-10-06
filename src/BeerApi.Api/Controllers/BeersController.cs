using BeerApi.Application.DTOs;
using BeerApi.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeerApi.Api.Controllers;

[ApiController]
[Route("api/beers")]
[Authorize]
public sealed class BeersController(IBeerService beerService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] BeerSearchRequestDto query, CancellationToken ct) =>
        Ok(await beerService.SearchAsync(query, query.Page, query.PageSize, ct));

    [HttpGet("cursor")]
    public async Task<IActionResult> SearchCursor([FromQuery] BeerCursorSearchRequestDto query, CancellationToken ct) =>
        Ok(await beerService.SearchCursorAsync(query, query.PageSize, query.Cursor, ct));
}