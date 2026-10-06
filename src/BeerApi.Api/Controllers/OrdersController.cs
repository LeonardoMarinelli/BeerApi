using System.Security.Claims;
using BeerApi.Api.Authorization;
using BeerApi.Application.DTOs;
using BeerApi.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeerApi.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public sealed class OrdersController(IOrderService orderService, IAuthorizationService authorizationService) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.WholesalerOrAdmin)]
    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto, CancellationToken ct)
    {
        int wholesalerId;
        if (User.IsInRole("Admin"))
        {
            if (dto.WholesalerId is null)
                return BadRequest(new ProblemDetails { Title = "WholesalerId é obrigatório para Admin." });
            wholesalerId = dto.WholesalerId.Value;
        }
        else
        {
            if (!int.TryParse(User.FindFirstValue("WholesalerId"), out wholesalerId))
                return Forbid();
            if (dto.WholesalerId.HasValue && dto.WholesalerId.Value != wholesalerId)
                return Forbid();
        }

        var order = await orderService.CreateAsync(wholesalerId, dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] OrderListQueryDto query, CancellationToken ct)
    {
        int? breweryId = query.BreweryId;
        int? wholesalerId = query.WholesalerId;

        if (!User.IsInRole("Admin"))
        {
            if (User.IsInRole("Brewer"))
            {
                if (!int.TryParse(User.FindFirstValue("BreweryId"), out var ownBreweryId) ||
                    breweryId.HasValue && breweryId.Value != ownBreweryId)
                    return Forbid();
                breweryId = ownBreweryId;
            }
            else if (User.IsInRole("Wholesaler"))
            {
                if (!int.TryParse(User.FindFirstValue("WholesalerId"), out var ownWholesalerId) ||
                    wholesalerId.HasValue && wholesalerId.Value != ownWholesalerId)
                    return Forbid();
                wholesalerId = ownWholesalerId;
            }
            else
            {
                return Forbid();
            }
        }

        var filters = new OrderSearchFiltersDto(breweryId, wholesalerId, query.Status);
        return Ok(await orderService.GetAllAsync(filters, query.Page, query.PageSize, ct));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var order = await orderService.GetByIdAsync(id, ct);
        return await CanAccessAsync(order) ? Ok(order) : Forbid();
    }

    [HttpPost("{id:int}/confirm")]
    [Authorize(Policy = AuthorizationPolicies.BrewerOrAdmin)]
    public Task<IActionResult> Confirm(int id, CancellationToken ct) =>
        TransitionAsync(id, orderService.ConfirmAsync, ct);

    [HttpPost("{id:int}/ship")]
    [Authorize(Policy = AuthorizationPolicies.BrewerOrAdmin)]
    public Task<IActionResult> Ship(int id, CancellationToken ct) =>
        TransitionAsync(id, orderService.ShipAsync, ct);

    [HttpPost("{id:int}/deliver")]
    [Authorize(Policy = AuthorizationPolicies.WholesalerOrAdmin)]
    public Task<IActionResult> Deliver(int id, CancellationToken ct) =>
        TransitionAsync(id, orderService.DeliverAsync, ct);

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, [FromBody] CancelOrderRequestDto? dto, CancellationToken ct)
    {
        var current = await orderService.GetByIdAsync(id, ct);
        if (!await CanAccessAsync(current))
            return Forbid();
        return Ok(await orderService.CancelAsync(id, dto?.Reason, ct));
    }

    private async Task<IActionResult> TransitionAsync(int id, Func<int, CancellationToken, Task<OrderDto>> transition, CancellationToken ct)
    {
        var current = await orderService.GetByIdAsync(id, ct);
        if (!await CanAccessAsync(current))
            return Forbid();
        return Ok(await transition(id, ct));
    }

    private async Task<bool> CanAccessAsync(OrderDto order) =>
        (await authorizationService.AuthorizeAsync(
            User,
            new OrderAccessResource(order.BreweryId, order.WholesalerId),
            AuthorizationPolicies.OrderParty)).Succeeded;
}