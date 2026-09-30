namespace BeerApi.Api.Authorization;

public static class AuthorizationPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string BrewerOrAdmin = "BrewerOrAdmin";
    public const string ManageBrewery = "ManageBrewery";
}