using BeerApi.Application.DTOs;
using BeerApi.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeerApi.Api.Controllers;

[ApiController]
[Route("api/wholesalers")]
[Authorize]
public class WholesalersController(WholesalerService wholesalerService) : ControllerBase
{
    private readonly WholesalerService _wholesalerService = wholesalerService;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationQueryDto query, CancellationToken ct) =>
        Ok(await _wholesalerService.GetAllAsync(query.Page, query.PageSize, ct));

    [HttpGet("{id:int}/beers")]
    public async Task<IActionResult> GetStock(int id, CancellationToken ct) =>
        Ok(await _wholesalerService.GetStockByWholesalerIdAsync(id, ct));

    [HttpPost("{id:int}/quote")]
    public async Task<IActionResult> GetQuote(int id, [FromBody] QuoteRequestDto request, CancellationToken ct)
    {
        var quote = await _wholesalerService.GetQuoteAsync(id, request, ct);
        return Ok(quote);
    }
}
