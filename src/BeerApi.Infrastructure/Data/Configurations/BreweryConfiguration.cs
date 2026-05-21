using BeerApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeerApi.Infrastructure.Data.Configurations;

public class BreweryConfiguration : IEntityTypeConfiguration<Brewery>
{
    public void Configure(EntityTypeBuilder<Brewery> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Name).IsRequired().HasMaxLength(200);
        builder.Property(b => b.Country).HasMaxLength(100);
        builder.Property(b => b.Description).HasMaxLength(1000);

        builder.HasData(
            new Brewery { Id = 1, Name = "Abbaye de Leffe",            Country = "Bélgica", Description = "Famosa cervejaria de estilo abacial estabelecida em 1152, conhecida por suas ales encorpadas e condimentadas." },
            new Brewery { Id = 2, Name = "Brasserie Chimay",           Country = "Bélgica", Description = "Autêntica cervejaria trapista da Abadia de Scourmont, produzindo algumas das cervejas mais celebradas da Bélgica desde 1862." },
            new Brewery { Id = 3, Name = "Brasserie Duvel Moortgat",  Country = "Bélgica", Description = "Cervejaria familiar fundada em 1871, mundialmente famosa pelo Duvel, uma forte ale dourada de aparência enganosa." },
            new Brewery { Id = 4, Name = "Brouwerij Westmalle",       Country = "Bélgica", Description = "Cervejaria monacal trapista próxima a Antuérpia, criadora do estilo Tripel, produzindo desde 1836." },
            new Brewery { Id = 5, Name = "Brasserie de Rochefort",    Country = "Bélgica", Description = "Cervejaria trapista na Abadia de Saint-Remy, produzindo complexas ales escuras numeradas por sua densidade." },
            new Brewery { Id = 6, Name = "Brouwerij Huyghe",          Country = "Bélgica", Description = "Cervejaria independente fundada em 1906 em Melle, ícone da linha Delirium com o elefante rosa." },
            new Brewery { Id = 7, Name = "AB InBev",                  Country = "Bélgica", Description = "Maior cervejaria do mundo, sediada em Leuven, lar de marcas globais icônicas como Stella Artois e Hoegaarden." }
        );
    }
}
