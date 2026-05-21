using BeerApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeerApi.Infrastructure.Data.Configurations;

public class WholesalerConfiguration : IEntityTypeConfiguration<Wholesaler>
{
    public void Configure(EntityTypeBuilder<Wholesaler> builder)
    {
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Name).IsRequired().HasMaxLength(200);
        builder.Property(w => w.Address).HasMaxLength(500);

        builder.HasData(
            new Wholesaler { Id = 1, Name = "BeerWorld Brussels",      Address = "Rue du Marché aux Herbes 75, 1000 Brussels" },
            new Wholesaler { Id = 2, Name = "Belgian Beer Wholesale",  Address = "Grote Markt 12, 2000 Antwerp" },
            new Wholesaler { Id = 3, Name = "Drinks & Co Antwerp",     Address = "Meir 20, 2000 Antwerp" }
        );
    }
}
