using BeerApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeerApi.Infrastructure.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(order => order.Id);
        builder.Property(order => order.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
        builder.Property(order => order.CancelReason).HasMaxLength(500);
        builder.Property(order => order.TotalBeforeDiscount).HasPrecision(12, 2);
        builder.Property(order => order.DiscountPercent).HasPrecision(5, 2);
        builder.Property(order => order.DiscountAmount).HasPrecision(12, 2);
        builder.Property(order => order.TotalPrice).HasPrecision(12, 2);
        builder.Property(order => order.Version).IsConcurrencyToken();
        builder.HasIndex(order => order.Status);
        builder.HasIndex(order => new { order.BreweryId, order.CreatedAt });
        builder.HasIndex(order => new { order.WholesalerId, order.CreatedAt });
        builder.HasOne(order => order.Brewery)
            .WithMany()
            .HasForeignKey(order => order.BreweryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(order => order.Wholesaler)
            .WithMany()
            .HasForeignKey(order => order.WholesalerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(order => order.Items)
            .WithOne(item => item.Order)
            .HasForeignKey(item => item.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}