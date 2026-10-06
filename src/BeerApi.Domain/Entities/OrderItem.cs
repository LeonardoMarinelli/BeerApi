namespace BeerApi.Domain.Entities;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int BeerId { get; set; }
    public string BeerName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountedSubtotal { get; set; }
    public Order Order { get; set; } = null!;
    public Beer Beer { get; set; } = null!;
}