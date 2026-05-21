using BeerApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeerApi.Infrastructure.Data.Configurations;

public class BeerConfiguration : IEntityTypeConfiguration<Beer>
{
    public void Configure(EntityTypeBuilder<Beer> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Name).IsRequired().HasMaxLength(200);
        builder.Property(b => b.Description).HasMaxLength(1000);
        builder.Property(b => b.AlcoholContent).HasPrecision(4, 2);
        builder.Property(b => b.Price).HasPrecision(10, 2);

        builder.HasOne(b => b.Brewery)
            .WithMany(br => br.Beers)
            .HasForeignKey(b => b.BreweryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasData(
            new Beer { Id = 1,  Name = "Leffe Blonde",       Description = "Uma ale abacial suave e equilibrada com notas de especiarias e baunilha.",                        AlcoholContent = 6.6m,  Price = 2.50m, BreweryId = 1 },
            new Beer { Id = 2,  Name = "Leffe Brune",        Description = "Uma ale abacial escura e encorpada com notas de caramelo e chocolate.",                          AlcoholContent = 6.5m,  Price = 2.50m, BreweryId = 1 },
            new Beer { Id = 3,  Name = "Leffe Triple",       Description = "Uma triple dourada e forte com caráter frutado e condimentado complexo.",                        AlcoholContent = 8.5m,  Price = 3.00m, BreweryId = 1 },
            new Beer { Id = 4,  Name = "Chimay Rouge",       Description = "Chimay Tampa Vermelha — um clássico dubbel trapista com notas frutadas e carameladas.",          AlcoholContent = 7.0m,  Price = 4.50m, BreweryId = 2 },
            new Beer { Id = 5,  Name = "Chimay Bleue",       Description = "Chimay Tampa Azul — uma ale escura encorpada e complexa com frutas escuras.",                    AlcoholContent = 9.0m,  Price = 5.00m, BreweryId = 2 },
            new Beer { Id = 6,  Name = "Chimay Triple",      Description = "Chimay Tampa Branca — uma Tripel dourada com final seco e lupulado.",                            AlcoholContent = 8.0m,  Price = 4.80m, BreweryId = 2 },
            new Beer { Id = 7,  Name = "Duvel",              Description = "A icônica ale dourada forte belga — 'diabo' em flamengo.",                                       AlcoholContent = 8.5m,  Price = 4.00m, BreweryId = 3 },
            new Beer { Id = 8,  Name = "Vedett Extra White", Description = "Uma wit beer belga refrescante com notas de cítrico e coentro.",                                 AlcoholContent = 4.7m,  Price = 2.80m, BreweryId = 3 },
            new Beer { Id = 9,  Name = "Westmalle Dubbel",  Description = "Um clássico dubbel trapista com malte rico, frutas escuras e final seco.",                        AlcoholContent = 7.0m,  Price = 3.50m, BreweryId = 4 },
            new Beer { Id = 10, Name = "Westmalle Tripel",  Description = "O estilo Tripel original — dourado, forte e maravilhosamente complexo.",                          AlcoholContent = 9.5m,  Price = 4.00m, BreweryId = 4 },
            new Beer { Id = 11, Name = "Rochefort 6",        Description = "O mais leve da linha Rochefort, ainda lindamente complexo com frutas âmbar.",                    AlcoholContent = 7.5m,  Price = 4.00m, BreweryId = 5 },
            new Beer { Id = 12, Name = "Rochefort 8",        Description = "Ale escura rica e complexa com ameixas, figos e uma força reconfortante.",                       AlcoholContent = 9.2m,  Price = 4.50m, BreweryId = 5 },
            new Beer { Id = 13, Name = "Rochefort 10",       Description = "Uma das maiores cervejas do mundo — profundamente complexa, reconfortante e satisfatória.",       AlcoholContent = 11.3m, Price = 5.50m, BreweryId = 5 },
            new Beer { Id = 14, Name = "Delirium Tremens",   Description = "Mundialmente famosa ale dourada forte com a característica garrafa do elefante rosa.",            AlcoholContent = 8.5m,  Price = 4.20m, BreweryId = 6 },
            new Beer { Id = 15, Name = "Stella Artois",      Description = "Um lager belga premium, fresco e refrescante, produzido desde 1926.",                            AlcoholContent = 5.2m,  Price = 1.80m, BreweryId = 7 },
            new Beer { Id = 16, Name = "Hoegaarden",         Description = "A witbier belga original, produzida com coentro e casca de laranja.",                            AlcoholContent = 4.9m,  Price = 2.20m, BreweryId = 7 }
        );
    }
}
