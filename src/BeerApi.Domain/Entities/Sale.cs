namespace BeerApi.Domain.Entities;

/// <summary>Represents a brewery selling a beer to a wholesaler.</summary>
public class Sale
{
    public int Id { get; set; }
    public int BreweryId { get; set; }
    public int WholesalerId { get; set; }
    public int BeerId { get; set; }
    public int Quantity { get; set; }
    public decimal PricePerUnit { get; set; }
    public decimal TotalPrice { get; set; }

    /// <summary>Tax rate applied to the sale. Currently 0 (tax-free), kept open for future use.</summary>
    public decimal TaxRate { get; set; } = 0m;

    public DateTime SaleDate { get; set; } = DateTime.UtcNow;

    public Brewery Brewery { get; set; } = null!;
    public Wholesaler Wholesaler { get; set; } = null!;
    public Beer Beer { get; set; } = null!;
}
