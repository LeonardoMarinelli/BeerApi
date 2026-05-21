namespace BeerApi.Domain.Entities;

/// <summary>Represents a beer in a wholesaler's stock (join entity with stock quantity).</summary>
public class WholesalerBeer
{
    public int WholesalerId { get; set; }
    public int BeerId { get; set; }

    /// <summary>Current stock quantity held by this wholesaler.</summary>
    public int Quantity { get; set; }

    public Wholesaler Wholesaler { get; set; } = null!;
    public Beer Beer { get; set; } = null!;
}
