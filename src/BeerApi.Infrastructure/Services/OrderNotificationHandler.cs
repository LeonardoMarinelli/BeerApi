using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BeerApi.Domain.Events;
using BeerApi.Domain.Enums;
using BeerApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BeerApi.Infrastructure.Services;

public sealed class OrderNotificationHandler(AppDbContext context, IMailSender mailSender)
{
    public async Task HandleAsync(Guid messageId, string eventType, string payload, CancellationToken ct = default)
    {
        switch (eventType)
        {
            case nameof(OrderPlacedEvent):
                await NotifyBreweryAsync(messageId, JsonSerializer.Deserialize<OrderPlacedEvent>(payload)!, ct);
                break;
            case nameof(OrderStatusChangedEvent):
                await NotifyWholesalerAsync(messageId, JsonSerializer.Deserialize<OrderStatusChangedEvent>(payload)!, ct);
                break;
            case nameof(StockLowEvent):
                await NotifyLowStockAsync(messageId, JsonSerializer.Deserialize<StockLowEvent>(payload)!, ct);
                break;
            default:
                throw new InvalidOperationException($"Unknown notification event '{eventType}'.");
        }
    }

    private async Task NotifyBreweryAsync(Guid messageId, OrderPlacedEvent domainEvent, CancellationToken ct)
    {
        var order = await context.Orders.AsNoTracking()
            .Include(item => item.Brewery)
            .FirstAsync(item => item.Id == domainEvent.OrderId, ct);
        var recipients = await context.Users.AsNoTracking()
            .Where(user => user.BreweryId == order.BreweryId && user.EmailConfirmed && user.Email != null)
            .Select(user => user.Email!)
            .ToListAsync(ct);

        foreach (var email in recipients)
            await SendOnceAsync(messageId, email, $"Novo pedido #{order.Id}",
                $"A cervejaria {order.Brewery.Name} recebeu um novo pedido. Confira o pedido #{order.Id}.", ct);
    }

    private async Task NotifyWholesalerAsync(Guid messageId, OrderStatusChangedEvent domainEvent, CancellationToken ct)
    {
        var order = await context.Orders.AsNoTracking()
            .Include(item => item.Wholesaler)
            .FirstAsync(item => item.Id == domainEvent.OrderId, ct);
        var recipients = await context.Users.AsNoTracking()
            .Where(user => user.WholesalerId == order.WholesalerId && user.EmailConfirmed && user.Email != null)
            .Select(user => user.Email!)
            .ToListAsync(ct);

        foreach (var email in recipients)
            await SendOnceAsync(messageId, email, $"Pedido #{order.Id}: {domainEvent.CurrentStatus}",
                $"O status do pedido #{order.Id} mudou de {domainEvent.PreviousStatus} para {domainEvent.CurrentStatus}.", ct);
    }

    private async Task NotifyLowStockAsync(Guid messageId, StockLowEvent domainEvent, CancellationToken ct)
    {
        var wholesaler = await context.Wholesalers.AsNoTracking()
            .FirstAsync(item => item.Id == domainEvent.WholesalerId, ct);
        var beer = await context.Beers.AsNoTracking()
            .FirstAsync(item => item.Id == domainEvent.BeerId, ct);
        var recipients = await context.Users.AsNoTracking()
            .Where(user => user.WholesalerId == domainEvent.WholesalerId && user.EmailConfirmed && user.Email != null)
            .Select(user => user.Email!)
            .ToListAsync(ct);

        foreach (var email in recipients)
            await SendOnceAsync(messageId, email, $"Estoque baixo: {beer.Name}",
                $"O estoque de {beer.Name} no atacadista {wholesaler.Name} está em {domainEvent.Quantity} unidades.", ct);
    }

    private async Task SendOnceAsync(Guid messageId, string email, string subject, string body, CancellationToken ct)
    {
        var recipientHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(email.ToUpperInvariant())));
        var alreadySent = await context.ProcessedMessageRecipients.AnyAsync(
            item => item.MessageId == messageId &&
                    item.Consumer == "notifications" &&
                    item.RecipientHash == recipientHash,
            ct);
        if (alreadySent)
            return;

        await mailSender.SendAsync(email, subject, body, ct);
        context.ProcessedMessageRecipients.Add(new ProcessedMessageRecipient
        {
            MessageId = messageId,
            Consumer = "notifications",
            RecipientHash = recipientHash
        });
        await context.SaveChangesAsync(ct);
    }
}