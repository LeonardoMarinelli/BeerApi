using BeerApi.Domain.Entities;
using BeerApi.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeerApi.Infrastructure.Data.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FirstName).HasMaxLength(100);
        builder.Property(u => u.LastName).HasMaxLength(100);

        builder.HasOne<Brewery>()
            .WithMany()
            .HasForeignKey(u => u.BreweryId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<Wholesaler>()
            .WithMany()
            .HasForeignKey(u => u.WholesalerId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
