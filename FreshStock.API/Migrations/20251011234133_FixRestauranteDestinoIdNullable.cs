using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FreshStock.API.Migrations
{
    /// <inheritdoc />
    public partial class FixRestauranteDestinoIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "RestauranteDestinoId",
                table: "MovimientosInventario",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "RestauranteDestinoId",
                table: "MovimientosInventario",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
