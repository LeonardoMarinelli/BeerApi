using BeerApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeerApi.Infrastructure.Data.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasKey(item => item.Id);
        builder.Property(item => item.BeerName).HasMaxLength(200).IsRequired();
        builder.Property(item => item.UnitPrice).HasPrecision(10, 2);
        builder.Property(item => item.Subtotal).HasPrecision(12, 2);
        builder.Property(item => item.DiscountedSubtotal).HasPrecision(12, 2);
        builder.HasOne(item => item.Beer)
            .WithMany()
            .HasForeignKey(item => item.BeerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => item.BeerId);
    }
}