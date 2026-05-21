namespace BeerApi.Domain.Entities;

public class Beer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal AlcoholContent { get; set; }

    /// <summary>Fixed price set by the brewery.</summary>
    public decimal Price { get; set; }

    public int BreweryId { get; set; }
    public Brewery Brewery { get; set; } = null!;

    public ICollection<WholesalerBeer> WholesalerBeers { get; set; } = new List<WholesalerBeer>();
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
