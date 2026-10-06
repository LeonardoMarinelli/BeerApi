using Microsoft.AspNetCore.Authorization;

namespace BeerApi.Api.Authorization;

public sealed class WholesalerOwnerAuthorizationHandler : AuthorizationHandler<WholesalerOwnerRequirement, int>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        WholesalerOwnerRequirement requirement,
        int wholesalerId)
    {
        if (context.User.IsInRole("Admin") ||
            context.User.IsInRole("Wholesaler") &&
            int.TryParse(context.User.FindFirst("WholesalerId")?.Value, out var userWholesalerId) &&
            userWholesalerId == wholesalerId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}