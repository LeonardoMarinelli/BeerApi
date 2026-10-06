using BeerApi.Infrastructure.Data;

namespace BeerApi.Infrastructure.Services.Messaging;

public interface IMessagePublisher
{
    Task PublishAsync(OutboxMessage message, CancellationToken ct = default);
}