using System.Security.Claims;
using BeerApi.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace BeerApi.Infrastructure.Identity;

/// <summary>
/// Injects BreweryId and WholesalerId as custom claims into the bearer token so controllers
/// can read them without extra database lookups.
/// </summary>
public class ApplicationUserClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<IdentityOptions> options)
        : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>(userManager, roleManager, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        if (user.BreweryId.HasValue)
            identity.AddClaim(new Claim("BreweryId", user.BreweryId.Value.ToString()));

        if (user.WholesalerId.HasValue)
            identity.AddClaim(new Claim("WholesalerId", user.WholesalerId.Value.ToString()));

        return identity;
    }
}
