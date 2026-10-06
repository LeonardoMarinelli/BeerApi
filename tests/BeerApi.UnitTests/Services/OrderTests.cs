using AwesomeAssertions;
using BeerApi.Domain.Entities;
using BeerApi.Domain.Enums;
using BeerApi.Domain.Exceptions;

namespace BeerApi.UnitTests.Services;

public class OrderTests
{
    [Fact]
    public void StatusTransitions_AdvanceInOrderAndRecordTimestamps()
    {
        var order = new Order();
        var now = DateTime.UtcNow;

        order.Confirm(now);
        order.Ship(now.AddMinutes(1));
        order.Deliver(now.AddMinutes(2));

        order.Status.Should().Be(OrderStatus.Delivered);
        order.ConfirmedAt.Should().Be(now);
        order.ShippedAt.Should().Be(now.AddMinutes(1));
        order.DeliveredAt.Should().Be(now.AddMinutes(2));
        order.Version.Should().Be(3);
    }

    [Fact]
    public void Confirm_WhenOrderIsNotPending_ThrowsConflictException()
    {
        var order = new Order();
        order.Confirm(DateTime.UtcNow);

        var act = () => order.Confirm(DateTime.UtcNow);

        act.Should().Throw<ConflictException>();
    }

    [Fact]
    public void Cancel_WhenShipped_ThrowsConflictException()
    {
        var order = new Order();
        order.Confirm(DateTime.UtcNow);
        order.Ship(DateTime.UtcNow);

        var act = () => order.Cancel("late", DateTime.UtcNow);

        act.Should().Throw<ConflictException>();
    }

    [Fact]
    public void Cancel_WhenPending_StoresReasonAndTimestamp()
    {
        var order = new Order();
        var now = DateTime.UtcNow;

        order.Cancel("  Pedido incorreto  ", now);

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.CancelReason.Should().Be("Pedido incorreto");
        order.CancelledAt.Should().Be(now);
        order.Version.Should().Be(1);
    }
}