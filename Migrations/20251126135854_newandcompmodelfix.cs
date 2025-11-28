using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GBazaar.Migrations
{
    /// <inheritdoc />
    public partial class newandcompmodelfix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalHistories_PurchaseRequests_PRID",
                table: "ApprovalHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_PaymentStatuses_PaymentStatusID",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_PRItems_SubCategories_SubCategoryID",
                table: "PRItems");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_POStatuses_POStatusID",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseRequests_PRStatuses_PRStatusID",
                table: "PurchaseRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Suppliers_PaymentTerms_PaymentTermsID",
                table: "Suppliers");

            migrationBuilder.DropTable(
                name: "PaymentStatuses");

            migrationBuilder.DropTable(
                name: "POStatuses");

            migrationBuilder.DropTable(
                name: "PRStatuses");

            migrationBuilder.DropTable(
                name: "SubCategories");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_TaxID",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseRequests_PRStatusID",
                table: "PurchaseRequests");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_POStatusID",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PRItems_SubCategoryID",
                table: "PRItems");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_InvoiceNumber",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_PaymentStatusID",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Departments_DepartmentName",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Budgets_DepartmentID",
                table: "Budgets");

            migrationBuilder.DropColumn(
                name: "SubCategoryID",
                table: "PRItems");

            migrationBuilder.RenameColumn(
                name: "UserName",
                table: "Users",
                newName: "Email");

            migrationBuilder.RenameIndex(
                name: "IX_Users_UserName",
                table: "Users",
                newName: "IX_Users_Email");

            migrationBuilder.RenameColumn(
                name: "PaymentTermsID",
                table: "Suppliers",
                newName: "PaymentTermID");

            migrationBuilder.RenameIndex(
                name: "IX_Suppliers_PaymentTermsID",
                table: "Suppliers",
                newName: "IX_Suppliers_PaymentTermID");

            migrationBuilder.RenameColumn(
                name: "PaymentTermsID",
                table: "PaymentTerms",
                newName: "PaymentTermID");

            migrationBuilder.RenameColumn(
                name: "AmountCommited",
                table: "Budgets",
                newName: "AmountCommitted");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<string>(
                name: "JobTitle",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactInfo",
                table: "Suppliers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RatedOn",
                table: "SupplierRatings",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<int>(
                name: "PRStatus",
                table: "PurchaseRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "POStatus",
                table: "PurchaseOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PRItemName",
                table: "PRItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UnitOfMeasure",
                table: "PRItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateOnly>(
                name: "DueDate",
                table: "Invoices",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PaymentDate",
                table: "Invoices",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentStatus",
                table: "Invoices",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateReceived",
                table: "GoodsReceipts",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<int>(
                name: "ManagerID",
                table: "Departments",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "EntityType",
                table: "Attachments",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "Attachments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "Attachments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "FileSizeBytes",
                table: "Attachments",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UploadDate",
                table: "Attachments",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AlterColumn<int>(
                name: "ActionType",
                table: "ApprovalHistories",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ActionDate",
                table: "ApprovalHistories",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    PermissionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PermissionName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.PermissionID);
                });

            migrationBuilder.CreateTable(
                name: "POItems",
                columns: table => new
                {
                    POItemID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    POID = table.Column<int>(type: "int", nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    QuantityOrdered = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POItems", x => x.POItemID);
                    table.ForeignKey(
                        name: "FK_POItems_PurchaseOrders_POID",
                        column: x => x.POID,
                        principalTable: "PurchaseOrders",
                        principalColumn: "POID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RoleID = table.Column<int>(type: "int", nullable: false),
                    PermissionID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleID, x.PermissionID });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionID",
                        column: x => x.PermissionID,
                        principalTable: "Permissions",
                        principalColumn: "PermissionID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleID",
                        column: x => x.RoleID,
                        principalTable: "Roles",
                        principalColumn: "RoleID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GoodsReceiptItems",
                columns: table => new
                {
                    GRItemID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GRID = table.Column<int>(type: "int", nullable: false),
                    POItemID = table.Column<int>(type: "int", nullable: false),
                    QuantityReceived = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GoodsReceiptItemGRItemID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReceiptItems", x => x.GRItemID);
                    table.ForeignKey(
                        name: "FK_GoodsReceiptItems_GoodsReceiptItems_GoodsReceiptItemGRItemID",
                        column: x => x.GoodsReceiptItemGRItemID,
                        principalTable: "GoodsReceiptItems",
                        principalColumn: "GRItemID");
                    table.ForeignKey(
                        name: "FK_GoodsReceiptItems_GoodsReceipts_GRID",
                        column: x => x.GRID,
                        principalTable: "GoodsReceipts",
                        principalColumn: "ReceiptID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoodsReceiptItems_POItems_POItemID",
                        column: x => x.POItemID,
                        principalTable: "POItems",
                        principalColumn: "POItemID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceNumber_SupplierID",
                table: "Invoices",
                columns: new[] { "InvoiceNumber", "SupplierID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_ManagerID",
                table: "Departments",
                column: "ManagerID");

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_DepartmentID_FiscalYear",
                table: "Budgets",
                columns: new[] { "DepartmentID", "FiscalYear" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_EntityType_EntityID_FileName",
                table: "Attachments",
                columns: new[] { "EntityType", "EntityID", "FileName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRules_MinAmount_MaxAmount_RequiredRoleID_ApprovalLevel",
                table: "ApprovalRules",
                columns: new[] { "MinAmount", "MaxAmount", "RequiredRoleID", "ApprovalLevel" },
                unique: true,
                filter: "[MaxAmount] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptItems_GoodsReceiptItemGRItemID",
                table: "GoodsReceiptItems",
                column: "GoodsReceiptItemGRItemID");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptItems_GRID",
                table: "GoodsReceiptItems",
                column: "GRID");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptItems_POItemID",
                table: "GoodsReceiptItems",
                column: "POItemID");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_PermissionName",
                table: "Permissions",
                column: "PermissionName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_POItems_POID",
                table: "POItems",
                column: "POID");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionID",
                table: "RolePermissions",
                column: "PermissionID");

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalHistories_PurchaseRequests_PRID",
                table: "ApprovalHistories",
                column: "PRID",
                principalTable: "PurchaseRequests",
                principalColumn: "PRID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Users_ManagerID",
                table: "Departments",
                column: "ManagerID",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Suppliers_PaymentTerms_PaymentTermID",
                table: "Suppliers",
                column: "PaymentTermID",
                principalTable: "PaymentTerms",
                principalColumn: "PaymentTermID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalHistories_PurchaseRequests_PRID",
                table: "ApprovalHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Users_ManagerID",
                table: "Departments");

            migrationBuilder.DropForeignKey(
                name: "FK_Suppliers_PaymentTerms_PaymentTermID",
                table: "Suppliers");

            migrationBuilder.DropTable(
                name: "GoodsReceiptItems");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "POItems");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_InvoiceNumber_SupplierID",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Departments_ManagerID",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Budgets_DepartmentID_FiscalYear",
                table: "Budgets");

            migrationBuilder.DropIndex(
                name: "IX_Attachments_EntityType_EntityID_FileName",
                table: "Attachments");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalRules_MinAmount_MaxAmount_RequiredRoleID_ApprovalLevel",
                table: "ApprovalRules");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "JobTitle",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastLoginAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ContactInfo",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "RatedOn",
                table: "SupplierRatings");

            migrationBuilder.DropColumn(
                name: "PRStatus",
                table: "PurchaseRequests");

            migrationBuilder.DropColumn(
                name: "POStatus",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "PRItemName",
                table: "PRItems");

            migrationBuilder.DropColumn(
                name: "UnitOfMeasure",
                table: "PRItems");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "PaymentDate",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ManagerID",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "FileSizeBytes",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "UploadDate",
                table: "Attachments");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "Users",
                newName: "UserName");

            migrationBuilder.RenameIndex(
                name: "IX_Users_Email",
                table: "Users",
                newName: "IX_Users_UserName");

            migrationBuilder.RenameColumn(
                name: "PaymentTermID",
                table: "Suppliers",
                newName: "PaymentTermsID");

            migrationBuilder.RenameIndex(
                name: "IX_Suppliers_PaymentTermID",
                table: "Suppliers",
                newName: "IX_Suppliers_PaymentTermsID");

            migrationBuilder.RenameColumn(
                name: "PaymentTermID",
                table: "PaymentTerms",
                newName: "PaymentTermsID");

            migrationBuilder.RenameColumn(
                name: "AmountCommitted",
                table: "Budgets",
                newName: "AmountCommited");

            migrationBuilder.AddColumn<int>(
                name: "SubCategoryID",
                table: "PRItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateReceived",
                table: "GoodsReceipts",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AlterColumn<string>(
                name: "EntityType",
                table: "Attachments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "ActionType",
                table: "ApprovalHistories",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ActionDate",
                table: "ApprovalHistories",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.CategoryID);
                });

            migrationBuilder.CreateTable(
                name: "PaymentStatuses",
                columns: table => new
                {
                    StatusID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StatusType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentStatuses", x => x.StatusID);
                });

            migrationBuilder.CreateTable(
                name: "POStatuses",
                columns: table => new
                {
                    StatusID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StatusType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POStatuses", x => x.StatusID);
                });

            migrationBuilder.CreateTable(
                name: "PRStatuses",
                columns: table => new
                {
                    StatusID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StatusType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PRStatuses", x => x.StatusID);
                });

            migrationBuilder.CreateTable(
                name: "SubCategories",
                columns: table => new
                {
                    SubCategoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryID = table.Column<int>(type: "int", nullable: false),
                    SubCategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubCategories", x => x.SubCategoryID);
                    table.ForeignKey(
                        name: "FK_SubCategories_Categories_CategoryID",
                        column: x => x.CategoryID,
                        principalTable: "Categories",
                        principalColumn: "CategoryID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_TaxID",
                table: "Suppliers",
                column: "TaxID",
                unique: true,
                filter: "[TaxID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequests_PRStatusID",
                table: "PurchaseRequests",
                column: "PRStatusID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_POStatusID",
                table: "PurchaseOrders",
                column: "POStatusID");

            migrationBuilder.CreateIndex(
                name: "IX_PRItems_SubCategoryID",
                table: "PRItems",
                column: "SubCategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceNumber",
                table: "Invoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_PaymentStatusID",
                table: "Invoices",
                column: "PaymentStatusID");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_DepartmentName",
                table: "Departments",
                column: "DepartmentName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_DepartmentID",
                table: "Budgets",
                column: "DepartmentID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_CategoryName",
                table: "Categories",
                column: "CategoryName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentStatuses_StatusType",
                table: "PaymentStatuses",
                column: "StatusType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_POStatuses_StatusType",
                table: "POStatuses",
                column: "StatusType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PRStatuses_StatusType",
                table: "PRStatuses",
                column: "StatusType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubCategories_CategoryID",
                table: "SubCategories",
                column: "CategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_SubCategories_SubCategoryName",
                table: "SubCategories",
                column: "SubCategoryName",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalHistories_PurchaseRequests_PRID",
                table: "ApprovalHistories",
                column: "PRID",
                principalTable: "PurchaseRequests",
                principalColumn: "PRID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_PaymentStatuses_PaymentStatusID",
                table: "Invoices",
                column: "PaymentStatusID",
                principalTable: "PaymentStatuses",
                principalColumn: "StatusID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PRItems_SubCategories_SubCategoryID",
                table: "PRItems",
                column: "SubCategoryID",
                principalTable: "SubCategories",
                principalColumn: "SubCategoryID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_POStatuses_POStatusID",
                table: "PurchaseOrders",
                column: "POStatusID",
                principalTable: "POStatuses",
                principalColumn: "StatusID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseRequests_PRStatuses_PRStatusID",
                table: "PurchaseRequests",
                column: "PRStatusID",
                principalTable: "PRStatuses",
                principalColumn: "StatusID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Suppliers_PaymentTerms_PaymentTermsID",
                table: "Suppliers",
                column: "PaymentTermsID",
                principalTable: "PaymentTerms",
                principalColumn: "PaymentTermsID",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
