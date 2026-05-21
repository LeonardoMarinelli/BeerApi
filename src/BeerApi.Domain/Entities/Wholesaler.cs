namespace BeerApi.Domain.Entities;

public class Wholesaler
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    public ICollection<WholesalerBeer> WholesalerBeers { get; set; } = new List<WholesalerBeer>();
}
