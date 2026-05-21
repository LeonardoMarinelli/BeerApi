using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeerApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class PtBrSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 1,
                column: "Description",
                value: "Uma ale abacial suave e equilibrada com notas de especiarias e baunilha.");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 2,
                column: "Description",
                value: "Uma ale abacial escura e encorpada com notas de caramelo e chocolate.");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 3,
                column: "Description",
                value: "Uma triple dourada e forte com caráter frutado e condimentado complexo.");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 4,
                column: "Description",
                value: "Chimay Tampa Vermelha — um clássico dubbel trapista com notas frutadas e carameladas.");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 5,
                column: "Description",
                value: "Chimay Tampa Azul — uma ale escura encorpada e complexa com frutas escuras.");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 6,
                column: "Description",
                value: "Chimay Tampa Branca — uma Tripel dourada com final seco e lupulado.");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 7,
                column: "Description",
                value: "A icônica ale dourada forte belga — 'diabo' em flamengo.");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 8,
                column: "Description",
                value: "Uma wit beer belga refrescante com notas de cítrico e coentro.");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 9,
                column: "Description",
                value: "Um clássico dubbel trapista com malte rico, frutas escuras e final seco.");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 10,
                column: "Description",
                value: "O estilo Tripel original — dourado, forte e maravilhosamente complexo.");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 11,
                column: "Description",
                value: "O mais leve da linha Rochefort, ainda lindamente complexo com frutas âmbar.");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 12,
                column: "Description",
                value: "Ale escura rica e complexa com ameixas, figos e uma força reconfortante.");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 13,
                column: "Description",
                value: "Uma das maiores cervejas do mundo — profundamente complexa, reconfortante e satisfatória.");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 14,
                column: "Description",
                value: "Mundialmente famosa ale dourada forte com a característica garrafa do elefante rosa.");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 15,
                column: "Description",
                value: "Um lager belga premium, fresco e refrescante, produzido desde 1926.");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 16,
                column: "Description",
                value: "A witbier belga original, produzida com coentro e casca de laranja.");

            migrationBuilder.UpdateData(
                table: "Breweries",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Country", "Description" },
                values: new object[] { "Bélgica", "Famosa cervejaria de estilo abacial estabelecida em 1152, conhecida por suas ales encorpadas e condimentadas." });

            migrationBuilder.UpdateData(
                table: "Breweries",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Country", "Description" },
                values: new object[] { "Bélgica", "Autêntica cervejaria trapista da Abadia de Scourmont, produzindo algumas das cervejas mais celebradas da Bélgica desde 1862." });

            migrationBuilder.UpdateData(
                table: "Breweries",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Country", "Description" },
                values: new object[] { "Bélgica", "Cervejaria familiar fundada em 1871, mundialmente famosa pelo Duvel, uma forte ale dourada de aparência enganosa." });

            migrationBuilder.UpdateData(
                table: "Breweries",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Country", "Description" },
                values: new object[] { "Bélgica", "Cervejaria monacal trapista próxima a Antuérpia, criadora do estilo Tripel, produzindo desde 1836." });

            migrationBuilder.UpdateData(
                table: "Breweries",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Country", "Description" },
                values: new object[] { "Bélgica", "Cervejaria trapista na Abadia de Saint-Remy, produzindo complexas ales escuras numeradas por sua densidade." });

            migrationBuilder.UpdateData(
                table: "Breweries",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Country", "Description" },
                values: new object[] { "Bélgica", "Cervejaria independente fundada em 1906 em Melle, ícone da linha Delirium com o elefante rosa." });

            migrationBuilder.UpdateData(
                table: "Breweries",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Country", "Description" },
                values: new object[] { "Bélgica", "Maior cervejaria do mundo, sediada em Leuven, lar de marcas globais icônicas como Stella Artois e Hoegaarden." });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 1,
                column: "Description",
                value: "A smooth, balanced abbey ale with hints of spice and vanilla.");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 2,
                column: "Description",
                value: "A dark, rich abbey ale with caramel and chocolate notes.");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 3,
                column: "Description",
                value: "A strong, golden triple with a complex fruity and spicy character.");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 4,
                column: "Description",
                value: "Chimay Red Cap — a classic Trappist dubbel with fruity, caramel notes.");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 5,
                column: "Description",
                value: "Chimay Blue Cap — a full-bodied, complex dark ale with dark fruit.");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 6,
                column: "Description",
                value: "Chimay White Cap — a golden Tripel with a dry, hoppy finish.");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 7,
                column: "Description",
                value: "The iconic Belgian strong golden ale — 'devil' in Flemish.");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 8,
                column: "Description",
                value: "A refreshing Belgian wit beer with citrus and coriander.");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 9,
                column: "Description",
                value: "A classic Trappist dubbel with rich malt, dark fruit and a dry finish.");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 10,
                column: "Description",
                value: "The original Tripel style — golden, strong and wonderfully complex.");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 11,
                column: "Description",
                value: "The lightest Rochefort, still beautifully complex with amber fruit.");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 12,
                column: "Description",
                value: "Rich and complex dark ale with prunes, figs and a warming strength.");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 13,
                column: "Description",
                value: "Among the world's greatest beers — deeply complex, warming and satisfying.");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 14,
                column: "Description",
                value: "World-famous strong golden ale with a distinctive pink elephant bottle.");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 15,
                column: "Description",
                value: "A premium Belgian lager, crisp and refreshing, brewed since 1926.");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 16,
                column: "Description",
                value: "The original Belgian witbier, brewed with coriander and orange peel.");

            migrationBuilder.UpdateData(
                table: "Breweries",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Country", "Description" },
                values: new object[] { "Belgium", "Famous Trappist-style abbey brewery established in 1152, known for its rich, spiced ales." });

            migrationBuilder.UpdateData(
                table: "Breweries",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Country", "Description" },
                values: new object[] { "Belgium", "Authentic Trappist brewery at the Scourmont Abbey, producing some of Belgium's most celebrated beers since 1862." });

            migrationBuilder.UpdateData(
                table: "Breweries",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Country", "Description" },
                values: new object[] { "Belgium", "Family brewery founded in 1871, world-renowned for Duvel, a deceptively strong golden ale." });

            migrationBuilder.UpdateData(
                table: "Breweries",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Country", "Description" },
                values: new object[] { "Belgium", "Trappist monastery brewery near Antwerp, the creator of the Tripel style, brewing since 1836." });

            migrationBuilder.UpdateData(
                table: "Breweries",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Country", "Description" },
                values: new object[] { "Belgium", "Trappist brewery within the Abbey of Saint-Remy, producing complex dark ales numbered by their density." });

            migrationBuilder.UpdateData(
                table: "Breweries",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Country", "Description" },
                values: new object[] { "Belgium", "Independent brewery founded in 1906 in Melle, iconic for the whimsical Delirium range with the pink elephant." });

            migrationBuilder.UpdateData(
                table: "Breweries",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Country", "Description" },
                values: new object[] { "Belgium", "World's largest brewer, headquartered in Leuven, home to iconic global brands like Stella Artois and Hoegaarden." });
        }
    }
}
