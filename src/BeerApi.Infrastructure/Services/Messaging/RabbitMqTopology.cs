using RabbitMQ.Client;

namespace BeerApi.Infrastructure.Services.Messaging;

public static class RabbitMqTopology
{
    public const string EventExchange = "beerapi.events";
    public const string DeadLetterExchange = "beerapi.events.dlx";
    public const string NotificationQueue = "beerapi.notifications";
    public const string DeadLetterQueue = "beerapi.notifications.dlq";

    public static async Task DeclareAsync(IChannel channel, CancellationToken ct = default)
    {
        await channel.ExchangeDeclareAsync(EventExchange, ExchangeType.Topic, durable: true, cancellationToken: ct);
        await channel.ExchangeDeclareAsync(DeadLetterExchange, ExchangeType.Direct, durable: true, cancellationToken: ct);
        await channel.QueueDeclareAsync(DeadLetterQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await channel.QueueBindAsync(DeadLetterQueue, DeadLetterExchange, DeadLetterQueue, cancellationToken: ct);

        var arguments = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = DeadLetterExchange,
            ["x-dead-letter-routing-key"] = DeadLetterQueue
        };
        await channel.QueueDeclareAsync(NotificationQueue, durable: true, exclusive: false, autoDelete: false, arguments: arguments, cancellationToken: ct);
        await channel.QueueBindAsync(NotificationQueue, EventExchange, "order.placed", cancellationToken: ct);
        await channel.QueueBindAsync(NotificationQueue, EventExchange, "order.status_changed", cancellationToken: ct);
        await channel.QueueBindAsync(NotificationQueue, EventExchange, "stock.low", cancellationToken: ct);
    }

    public static string GetRoutingKey(string eventType) => eventType switch
    {
        "OrderPlacedEvent" => "order.placed",
        "OrderStatusChangedEvent" => "order.status_changed",
        "StockLowEvent" => "stock.low",
        _ => throw new InvalidOperationException($"Unknown outbox event type '{eventType}'.")
    };
}