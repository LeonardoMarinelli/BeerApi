using BeerApi.Domain.Enums;
using BeerApi.Domain.Exceptions;

namespace BeerApi.Domain.Entities;

public class Order
{
    public int Id { get; set; }
    public int BreweryId { get; set; }
    public int WholesalerId { get; set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime? ShippedAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? CancelReason { get; private set; }
    public decimal TotalBeforeDiscount { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalPrice { get; set; }
    public int Version { get; private set; }
    public Brewery Brewery { get; set; } = null!;
    public Wholesaler Wholesaler { get; set; } = null!;
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

    public void Confirm(DateTime at)
    {
        EnsureStatus(OrderStatus.Pending);
        Status = OrderStatus.Confirmed;
        ConfirmedAt = at;
        Version++;
    }

    public void Ship(DateTime at)
    {
        EnsureStatus(OrderStatus.Confirmed);
        Status = OrderStatus.Shipped;
        ShippedAt = at;
        Version++;
    }

    public void Deliver(DateTime at)
    {
        EnsureStatus(OrderStatus.Shipped);
        Status = OrderStatus.Delivered;
        DeliveredAt = at;
        Version++;
    }

    public void Cancel(string? reason, DateTime at)
    {
        if (Status is not OrderStatus.Pending and not OrderStatus.Confirmed)
            throw new ConflictException($"Pedidos no estado {Status} não podem ser cancelados.");

        Status = OrderStatus.Cancelled;
        CancelledAt = at;
        CancelReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        Version++;
    }

    private void EnsureStatus(OrderStatus expected)
    {
        if (Status != expected)
            throw new ConflictException($"A transição não é permitida para pedidos no estado {Status}.");
    }
}