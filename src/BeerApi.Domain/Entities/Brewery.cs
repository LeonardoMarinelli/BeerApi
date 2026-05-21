namespace BeerApi.Domain.Entities;

public class Brewery
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public ICollection<Beer> Beers { get; set; } = new List<Beer>();
}
