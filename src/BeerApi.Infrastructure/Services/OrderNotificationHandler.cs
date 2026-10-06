using System.Text.Json;
using BeerApi.Domain.Events;
using BeerApi.Domain.Enums;
using BeerApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BeerApi.Infrastructure.Services;

public sealed class OrderNotificationHandler(AppDbContext context, IMailSender mailSender)
{
    public async Task HandleAsync(string eventType, string payload, CancellationToken ct = default)
    {
        switch (eventType)
        {
            case nameof(OrderPlacedEvent):
                await NotifyBreweryAsync(JsonSerializer.Deserialize<OrderPlacedEvent>(payload)!, ct);
                break;
            case nameof(OrderStatusChangedEvent):
                await NotifyWholesalerAsync(JsonSerializer.Deserialize<OrderStatusChangedEvent>(payload)!, ct);
                break;
            case nameof(StockLowEvent):
                await NotifyLowStockAsync(JsonSerializer.Deserialize<StockLowEvent>(payload)!, ct);
                break;
            default:
                throw new InvalidOperationException($"Unknown notification event '{eventType}'.");
        }
    }

    private async Task NotifyBreweryAsync(OrderPlacedEvent domainEvent, CancellationToken ct)
    {
        var order = await context.Orders.AsNoTracking()
            .Include(item => item.Brewery)
            .FirstAsync(item => item.Id == domainEvent.OrderId, ct);
        var recipients = await context.Users.AsNoTracking()
            .Where(user => user.BreweryId == order.BreweryId && user.EmailConfirmed && user.Email != null)
            .Select(user => user.Email!)
            .ToListAsync(ct);

        foreach (var email in recipients)
            await mailSender.SendAsync(email, $"Novo pedido #{order.Id}",
                $"A cervejaria {order.Brewery.Name} recebeu um novo pedido. Confira o pedido #{order.Id}.", ct);
    }

    private async Task NotifyWholesalerAsync(OrderStatusChangedEvent domainEvent, CancellationToken ct)
    {
        var order = await context.Orders.AsNoTracking()
            .Include(item => item.Wholesaler)
            .FirstAsync(item => item.Id == domainEvent.OrderId, ct);
        var recipients = await context.Users.AsNoTracking()
            .Where(user => user.WholesalerId == order.WholesalerId && user.EmailConfirmed && user.Email != null)
            .Select(user => user.Email!)
            .ToListAsync(ct);

        foreach (var email in recipients)
            await mailSender.SendAsync(email, $"Pedido #{order.Id}: {domainEvent.CurrentStatus}",
                $"O status do pedido #{order.Id} mudou de {domainEvent.PreviousStatus} para {domainEvent.CurrentStatus}.", ct);
    }

    private async Task NotifyLowStockAsync(StockLowEvent domainEvent, CancellationToken ct)
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
            await mailSender.SendAsync(email, $"Estoque baixo: {beer.Name}",
                $"O estoque de {beer.Name} no atacadista {wholesaler.Name} está em {domainEvent.Quantity} unidades.", ct);
    }
}