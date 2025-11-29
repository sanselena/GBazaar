using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GBazaar.Migrations
{
    /// <inheritdoc />
    public partial class supplierekle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SupplierID",
                table: "PurchaseRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SupplierID",
                table: "PRItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequests_SupplierID",
                table: "PurchaseRequests",
                column: "SupplierID");

            migrationBuilder.CreateIndex(
                name: "IX_PRItems_SupplierID",
                table: "PRItems",
                column: "SupplierID");

            migrationBuilder.AddForeignKey(
                name: "FK_PRItems_Suppliers_SupplierID",
                table: "PRItems",
                column: "SupplierID",
                principalTable: "Suppliers",
                principalColumn: "SupplierID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseRequests_Suppliers_SupplierID",
                table: "PurchaseRequests",
                column: "SupplierID",
                principalTable: "Suppliers",
                principalColumn: "SupplierID",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PRItems_Suppliers_SupplierID",
                table: "PRItems");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseRequests_Suppliers_SupplierID",
                table: "PurchaseRequests");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseRequests_SupplierID",
                table: "PurchaseRequests");

            migrationBuilder.DropIndex(
                name: "IX_PRItems_SupplierID",
                table: "PRItems");

            migrationBuilder.DropColumn(
                name: "SupplierID",
                table: "PurchaseRequests");

            migrationBuilder.DropColumn(
                name: "SupplierID",
                table: "PRItems");
        }
    }
}
