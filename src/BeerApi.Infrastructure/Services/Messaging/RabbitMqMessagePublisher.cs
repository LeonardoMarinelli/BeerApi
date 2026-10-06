using System.Text;
using BeerApi.Infrastructure.Data;
using RabbitMQ.Client;

namespace BeerApi.Infrastructure.Services.Messaging;

public sealed class RabbitMqMessagePublisher(RabbitMqConnectionProvider connectionProvider) : IMessagePublisher, IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IChannel? _channel;

    public async Task PublishAsync(OutboxMessage message, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var channel = await GetChannelAsync(ct);
            var properties = new BasicProperties
            {
                MessageId = message.Id.ToString("N"),
                Type = message.EventType,
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent
            };
            await channel.BasicPublishAsync(
                RabbitMqTopology.EventExchange,
                RabbitMqTopology.GetRoutingKey(message.EventType),
                mandatory: true,
                basicProperties: properties,
                body: Encoding.UTF8.GetBytes(message.Payload),
                cancellationToken: ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken ct)
    {
        if (_channel is { IsOpen: true })
            return _channel;

        if (_channel is not null)
            await _channel.DisposeAsync();

        var connection = await connectionProvider.GetConnectionAsync(ct);
        var options = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true);
        _channel = await connection.CreateChannelAsync(options, ct);
        await RabbitMqTopology.DeclareAsync(_channel, ct);
        return _channel;
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.DisposeAsync();
        _gate.Dispose();
    }
}