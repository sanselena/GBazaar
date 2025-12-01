using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GBazaar.Migrations
{
    /// <inheritdoc />
    public partial class fixx : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductID",
                table: "POItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_POItems_ProductID",
                table: "POItems",
                column: "ProductID");

            migrationBuilder.AddForeignKey(
                name: "FK_POItems_Products_ProductID",
                table: "POItems",
                column: "ProductID",
                principalTable: "Products",
                principalColumn: "ProductID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_POItems_Products_ProductID",
                table: "POItems");

            migrationBuilder.DropIndex(
                name: "IX_POItems_ProductID",
                table: "POItems");

            migrationBuilder.DropColumn(
                name: "ProductID",
                table: "POItems");
        }
    }
}
