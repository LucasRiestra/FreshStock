using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FreshStock.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCostoUnitarioToProducto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CostoUnitario",
                table: "Productos",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CostoUnitario",
                table: "Productos");
        }
    }
}
