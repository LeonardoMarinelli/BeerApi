using RabbitMQ.Client;

namespace BeerApi.Infrastructure.Services.Messaging;

public static class RabbitMqTopology
{
    public const string EventExchange = "beerapi.events";
    public const string DeadLetterExchange = "beerapi.events.dlx";
    public const string NotificationQueue = "beerapi.notifications";
    public const string OrderPlacedRetryQueue = "beerapi.notifications.retry.order_placed";
    public const string OrderStatusChangedRetryQueue = "beerapi.notifications.retry.order_status_changed";
    public const string StockLowRetryQueue = "beerapi.notifications.retry.stock_low";
    public const string DeadLetterQueue = "beerapi.notifications.dlq";

    public static async Task DeclareAsync(IChannel channel, CancellationToken ct = default)
    {
        await channel.ExchangeDeclareAsync(EventExchange, ExchangeType.Topic, durable: true, cancellationToken: ct);
        await channel.ExchangeDeclareAsync(DeadLetterExchange, ExchangeType.Direct, durable: true, cancellationToken: ct);
        await channel.QueueDeclareAsync(DeadLetterQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await channel.QueueBindAsync(DeadLetterQueue, DeadLetterExchange, DeadLetterQueue, cancellationToken: ct);

        await DeclareRetryQueueAsync(channel, OrderPlacedRetryQueue, "order.placed", ct);
        await DeclareRetryQueueAsync(channel, OrderStatusChangedRetryQueue, "order.status_changed", ct);
        await DeclareRetryQueueAsync(channel, StockLowRetryQueue, "stock.low", ct);

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

    public static string GetRetryQueue(string eventType) => eventType switch
    {
        "OrderPlacedEvent" => OrderPlacedRetryQueue,
        "OrderStatusChangedEvent" => OrderStatusChangedRetryQueue,
        "StockLowEvent" => StockLowRetryQueue,
        _ => throw new InvalidOperationException($"Unknown notification event type '{eventType}'.")
    };

    private static async Task DeclareRetryQueueAsync(IChannel channel, string queue, string routingKey, CancellationToken ct)
    {
        var arguments = new Dictionary<string, object?>
        {
            ["x-message-ttl"] = 5000,
            ["x-dead-letter-exchange"] = EventExchange,
            ["x-dead-letter-routing-key"] = routingKey
        };
        await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false, arguments: arguments, cancellationToken: ct);
    }
}