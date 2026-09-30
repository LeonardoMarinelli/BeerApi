using Microsoft.AspNetCore.Authorization;

namespace BeerApi.Api.Authorization;

public sealed class BreweryOwnerAuthorizationHandler : AuthorizationHandler<BreweryOwnerRequirement, int>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        BreweryOwnerRequirement requirement,
        int breweryId)
    {
        if (context.User.IsInRole("Admin") ||
            (int.TryParse(context.User.FindFirst("BreweryId")?.Value, out var userBreweryId) &&
             userBreweryId == breweryId))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}