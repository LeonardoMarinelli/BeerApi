using BeerApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeerApi.Infrastructure.Data.Configurations;

public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.PricePerUnit).HasPrecision(10, 2);
        builder.Property(s => s.TotalPrice).HasPrecision(10, 2);
        builder.Property(s => s.TaxRate).HasPrecision(5, 2);

        builder.HasOne(s => s.Brewery)
            .WithMany()
            .HasForeignKey(s => s.BreweryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Wholesaler)
            .WithMany()
            .HasForeignKey(s => s.WholesalerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Beer)
            .WithMany(b => b.Sales)
            .HasForeignKey(s => s.BeerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
