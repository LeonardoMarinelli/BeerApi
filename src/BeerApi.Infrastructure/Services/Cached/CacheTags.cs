namespace BeerApi.Infrastructure.Services.Cached;

internal static class CacheTags
{
    public const string Breweries = "breweries";
    public const string Wholesalers = "wholesalers";
    public const string WholesalerStock = "wholesaler-stock";

    public static string BreweryBeers(int breweryId) => $"brewery-beers:{breweryId}";
    public static string Beer(int beerId) => $"beer:{beerId}";
    public static string WholesalerStockFor(int wholesalerId) => $"wholesaler-stock:{wholesalerId}";
}