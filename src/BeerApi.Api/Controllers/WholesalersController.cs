using BeerApi.Application.DTOs;
using BeerApi.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeerApi.Api.Controllers;

[ApiController]
[Route("api/wholesalers")]
[Authorize]
public class WholesalersController(IWholesalerService wholesalerService) : ControllerBase
{
    private readonly IWholesalerService _wholesalerService = wholesalerService;

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _wholesalerService.GetAllAsync());

    [HttpGet("{id:int}/beers")]
    public async Task<IActionResult> GetStock(int id) =>
        Ok(await _wholesalerService.GetStockByWholesalerIdAsync(id));

    [HttpPost("{id:int}/quote")]
    public async Task<IActionResult> GetQuote(int id, [FromBody] QuoteRequestDto request)
    {
        var quote = await _wholesalerService.GetQuoteAsync(id, request);
        return Ok(quote);
    }
}
