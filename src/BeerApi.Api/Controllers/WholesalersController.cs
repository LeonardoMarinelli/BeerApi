using BeerApi.Application.DTOs;
using BeerApi.Application.Services.Interfaces;
using BeerApi.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeerApi.Api.Controllers;

[ApiController]
[Route("api/wholesalers")]
[Authorize]
public class WholesalersController(
    IWholesalerService wholesalerService,
    IStockService stockService,
    IAuthorizationService authorizationService) : ControllerBase
{
    private readonly IWholesalerService _wholesalerService = wholesalerService;
    private readonly IStockService _stockService = stockService;
    private readonly IAuthorizationService _authorizationService = authorizationService;

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

    [HttpPost("{id:int}/beers/{beerId:int}/stock-out")]
    [Authorize(Policy = AuthorizationPolicies.WholesalerOrAdmin)]
    public async Task<IActionResult> RemoveStock(int id, int beerId, [FromBody] StockOutRequestDto request, CancellationToken ct)
    {
        if (!(await _authorizationService.AuthorizeAsync(User, id, AuthorizationPolicies.ManageWholesaler)).Succeeded)
            return Forbid();

        await _stockService.RemoveAsync(id, beerId, request.Quantity, ct);
        return NoContent();
    }
}
