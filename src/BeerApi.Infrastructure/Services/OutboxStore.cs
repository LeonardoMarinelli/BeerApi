using System.Text.Json;
using BeerApi.Domain.Events;
using BeerApi.Domain.Interfaces;
using BeerApi.Infrastructure.Data;

namespace BeerApi.Infrastructure.Services;

public sealed class OutboxStore(AppDbContext context) : IOutbox
{
    public async Task AddAsync(IDomainEvent domainEvent, CancellationToken ct = default)
    {
        context.OutboxMessages.Add(new OutboxMessage
        {
            Id = domainEvent.EventId,
            EventType = domainEvent.GetType().Name,
            Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
            OccurredAt = domainEvent.OccurredAt
        });
        await context.SaveChangesAsync(ct);
    }
}