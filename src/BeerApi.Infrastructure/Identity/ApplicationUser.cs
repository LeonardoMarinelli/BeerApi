using Microsoft.AspNetCore.Identity;

namespace BeerApi.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    /// <summary>Set when the user is a Brewer. Links the user to their brewery.</summary>
    public int? BreweryId { get; set; }

    /// <summary>Set when the user is a Wholesaler. Links the user to their wholesale company.</summary>
    public int? WholesalerId { get; set; }
}
