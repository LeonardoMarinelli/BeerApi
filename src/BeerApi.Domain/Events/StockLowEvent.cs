namespace BeerApi.Domain.Events;

public sealed record StockLowEvent(int WholesalerId, int BeerId, int Quantity, int Threshold) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}