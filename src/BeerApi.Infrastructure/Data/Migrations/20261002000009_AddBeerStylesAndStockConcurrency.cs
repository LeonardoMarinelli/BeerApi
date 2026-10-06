using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeerApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBeerStylesAndStockConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "WholesalerBeers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Style",
                table: "Beers",
                type: "varchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Other")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 1,
                column: "Style",
                value: "Blonde");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 2,
                column: "Style",
                value: "AbbeyAle");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 3,
                column: "Style",
                value: "Tripel");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 4,
                column: "Style",
                value: "Dubbel");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 5,
                column: "Style",
                value: "Quadrupel");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 6,
                column: "Style",
                value: "Tripel");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 7,
                column: "Style",
                value: "StrongGolden");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 8,
                column: "Style",
                value: "Witbier");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 9,
                column: "Style",
                value: "Dubbel");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 10,
                column: "Style",
                value: "Tripel");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 11,
                column: "Style",
                value: "AbbeyAle");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 12,
                column: "Style",
                value: "Quadrupel");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 13,
                column: "Style",
                value: "Quadrupel");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 14,
                column: "Style",
                value: "StrongGolden");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 15,
                column: "Style",
                value: "Lager");

            migrationBuilder.UpdateData(
                table: "Beers",
                keyColumn: "Id",
                keyValue: 16,
                column: "Style",
                value: "Witbier");

            migrationBuilder.UpdateData(
                table: "WholesalerBeers",
                keyColumns: new[] { "BeerId", "WholesalerId" },
                keyValues: new object[] { 1, 1 },
                column: "Version",
                value: 0);

            migrationBuilder.UpdateData(
                table: "WholesalerBeers",
                keyColumns: new[] { "BeerId", "WholesalerId" },
                keyValues: new object[] { 4, 1 },
                column: "Version",
                value: 0);

            migrationBuilder.UpdateData(
                table: "WholesalerBeers",
                keyColumns: new[] { "BeerId", "WholesalerId" },
                keyValues: new object[] { 7, 1 },
                column: "Version",
                value: 0);

            migrationBuilder.UpdateData(
                table: "WholesalerBeers",
                keyColumns: new[] { "BeerId", "WholesalerId" },
                keyValues: new object[] { 15, 1 },
                column: "Version",
                value: 0);

            migrationBuilder.UpdateData(
                table: "WholesalerBeers",
                keyColumns: new[] { "BeerId", "WholesalerId" },
                keyValues: new object[] { 5, 2 },
                column: "Version",
                value: 0);

            migrationBuilder.UpdateData(
                table: "WholesalerBeers",
                keyColumns: new[] { "BeerId", "WholesalerId" },
                keyValues: new object[] { 10, 2 },
                column: "Version",
                value: 0);

            migrationBuilder.UpdateData(
                table: "WholesalerBeers",
                keyColumns: new[] { "BeerId", "WholesalerId" },
                keyValues: new object[] { 13, 2 },
                column: "Version",
                value: 0);

            migrationBuilder.UpdateData(
                table: "WholesalerBeers",
                keyColumns: new[] { "BeerId", "WholesalerId" },
                keyValues: new object[] { 14, 2 },
                column: "Version",
                value: 0);

            migrationBuilder.UpdateData(
                table: "WholesalerBeers",
                keyColumns: new[] { "BeerId", "WholesalerId" },
                keyValues: new object[] { 3, 3 },
                column: "Version",
                value: 0);

            migrationBuilder.UpdateData(
                table: "WholesalerBeers",
                keyColumns: new[] { "BeerId", "WholesalerId" },
                keyValues: new object[] { 8, 3 },
                column: "Version",
                value: 0);

            migrationBuilder.UpdateData(
                table: "WholesalerBeers",
                keyColumns: new[] { "BeerId", "WholesalerId" },
                keyValues: new object[] { 12, 3 },
                column: "Version",
                value: 0);

            migrationBuilder.UpdateData(
                table: "WholesalerBeers",
                keyColumns: new[] { "BeerId", "WholesalerId" },
                keyValues: new object[] { 16, 3 },
                column: "Version",
                value: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "WholesalerBeers");

            migrationBuilder.DropColumn(
                name: "Style",
                table: "Beers");
        }
    }
}
