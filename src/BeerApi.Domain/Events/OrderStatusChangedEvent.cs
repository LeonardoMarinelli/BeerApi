using BeerApi.Domain.Enums;

namespace BeerApi.Domain.Events;

public sealed record OrderStatusChangedEvent(int OrderId, OrderStatus PreviousStatus, OrderStatus CurrentStatus) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}