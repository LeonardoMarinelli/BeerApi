using BeerApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeerApi.Infrastructure.Data.Configurations;

public class WholesalerBeerConfiguration : IEntityTypeConfiguration<WholesalerBeer>
{
    public void Configure(EntityTypeBuilder<WholesalerBeer> builder)
    {
        builder.HasKey(wb => new { wb.WholesalerId, wb.BeerId });

        builder.HasOne(wb => wb.Wholesaler)
            .WithMany(w => w.WholesalerBeers)
            .HasForeignKey(wb => wb.WholesalerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(wb => wb.Beer)
            .WithMany(b => b.WholesalerBeers)
            .HasForeignKey(wb => wb.BeerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasData(
            new WholesalerBeer { WholesalerId = 1, BeerId = 1,  Quantity = 50  },  
            new WholesalerBeer { WholesalerId = 1, BeerId = 4,  Quantity = 30  },  
            new WholesalerBeer { WholesalerId = 1, BeerId = 7,  Quantity = 40  },  
            new WholesalerBeer { WholesalerId = 1, BeerId = 15, Quantity = 100 },  

            new WholesalerBeer { WholesalerId = 2, BeerId = 5,  Quantity = 20  },  
            new WholesalerBeer { WholesalerId = 2, BeerId = 10, Quantity = 25  },  
            new WholesalerBeer { WholesalerId = 2, BeerId = 13, Quantity = 15  },  
            new WholesalerBeer { WholesalerId = 2, BeerId = 14, Quantity = 35  },  

            new WholesalerBeer { WholesalerId = 3, BeerId = 3,  Quantity = 30  },  
            new WholesalerBeer { WholesalerId = 3, BeerId = 8,  Quantity = 45  },  
            new WholesalerBeer { WholesalerId = 3, BeerId = 12, Quantity = 20  },  
            new WholesalerBeer { WholesalerId = 3, BeerId = 16, Quantity = 60  }   
        );
    }
}
