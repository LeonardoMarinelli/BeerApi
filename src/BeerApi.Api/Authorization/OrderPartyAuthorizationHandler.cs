using Microsoft.AspNetCore.Authorization;

namespace BeerApi.Api.Authorization;

public sealed class OrderPartyAuthorizationHandler : AuthorizationHandler<OrderPartyRequirement, OrderAccessResource>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OrderPartyRequirement requirement,
        OrderAccessResource order)
    {
        if (context.User.IsInRole("Admin") ||
            (context.User.IsInRole("Brewer") && int.TryParse(context.User.FindFirst("BreweryId")?.Value, out var breweryId) && breweryId == order.BreweryId) ||
            (context.User.IsInRole("Wholesaler") && int.TryParse(context.User.FindFirst("WholesalerId")?.Value, out var wholesalerId) && wholesalerId == order.WholesalerId))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}