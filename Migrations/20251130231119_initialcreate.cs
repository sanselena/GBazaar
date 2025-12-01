using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GBazaar.Migrations
{
    /// <inheritdoc />
    public partial class initialcreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentTerms",
                columns: table => new
                {
                    PaymentTermID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DaysDue = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTerms", x => x.PaymentTermID);
                });

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
                name: "Roles",
                columns: table => new
                {
                    RoleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleID);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    SupplierID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContactInfo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TaxID = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    PaymentTermID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.SupplierID);
                    table.ForeignKey(
                        name: "FK_Suppliers_PaymentTerms_PaymentTermID",
                        column: x => x.PaymentTermID,
                        principalTable: "PaymentTerms",
                        principalColumn: "PaymentTermID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalRules",
                columns: table => new
                {
                    RuleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MinAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    RequiredRoleID = table.Column<int>(type: "int", nullable: false),
                    ApprovalLevel = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalRules", x => x.RuleID);
                    table.ForeignKey(
                        name: "FK_ApprovalRules_Roles_RequiredRoleID",
                        column: x => x.RequiredRoleID,
                        principalTable: "Roles",
                        principalColumn: "RoleID",
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
                name: "Products",
                columns: table => new
                {
                    ProductID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierID = table.Column<int>(type: "int", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.ProductID);
                    table.ForeignKey(
                        name: "FK_Products_Suppliers_SupplierID",
                        column: x => x.SupplierID,
                        principalTable: "Suppliers",
                        principalColumn: "SupplierID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalHistories",
                columns: table => new
                {
                    HistoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PRID = table.Column<int>(type: "int", nullable: false),
                    ApproverID = table.Column<int>(type: "int", nullable: false),
                    ActionDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ActionType = table.Column<int>(type: "int", nullable: false),
                    ApprovalLevel = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalHistories", x => x.HistoryID);
                });

            migrationBuilder.CreateTable(
                name: "Attachments",
                columns: table => new
                {
                    AttachmentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityType = table.Column<int>(type: "int", nullable: false),
                    EntityID = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    UploadedByUserID = table.Column<int>(type: "int", nullable: false),
                    UploadDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attachments", x => x.AttachmentID);
                });

            migrationBuilder.CreateTable(
                name: "Budgets",
                columns: table => new
                {
                    BudgetID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartmentID = table.Column<int>(type: "int", nullable: false),
                    FiscalYear = table.Column<int>(type: "int", nullable: false),
                    TotalBudget = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AmountCommitted = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Budgets", x => x.BudgetID);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    DepartmentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartmentName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BudgetCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ManagerID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.DepartmentID);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    JobTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RoleID = table.Column<int>(type: "int", nullable: false),
                    DepartmentID = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserID);
                    table.ForeignKey(
                        name: "FK_Users_Departments_DepartmentID",
                        column: x => x.DepartmentID,
                        principalTable: "Departments",
                        principalColumn: "DepartmentID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleID",
                        column: x => x.RoleID,
                        principalTable: "Roles",
                        principalColumn: "RoleID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseRequests",
                columns: table => new
                {
                    PRID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequesterID = table.Column<int>(type: "int", nullable: false),
                    DateSubmitted = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EstimatedTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PRStatusID = table.Column<int>(type: "int", nullable: false),
                    Justification = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SupplierID = table.Column<int>(type: "int", nullable: true),
                    PRStatus = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseRequests", x => x.PRID);
                    table.ForeignKey(
                        name: "FK_PurchaseRequests_Suppliers_SupplierID",
                        column: x => x.SupplierID,
                        principalTable: "Suppliers",
                        principalColumn: "SupplierID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PurchaseRequests_Users_RequesterID",
                        column: x => x.RequesterID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PRItems",
                columns: table => new
                {
                    PRItemID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PRID = table.Column<int>(type: "int", nullable: false),
                    PRItemName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SupplierID = table.Column<int>(type: "int", nullable: true),
                    ProductID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PRItems", x => x.PRItemID);
                    table.ForeignKey(
                        name: "FK_PRItems_Products_ProductID",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PRItems_PurchaseRequests_PRID",
                        column: x => x.PRID,
                        principalTable: "PurchaseRequests",
                        principalColumn: "PRID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PRItems_Suppliers_SupplierID",
                        column: x => x.SupplierID,
                        principalTable: "Suppliers",
                        principalColumn: "SupplierID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                columns: table => new
                {
                    POID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PRID = table.Column<int>(type: "int", nullable: false),
                    SupplierID = table.Column<int>(type: "int", nullable: false),
                    DateIssued = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequiredDeliveryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    POStatusID = table.Column<int>(type: "int", nullable: false),
                    POStatus = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrders", x => x.POID);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_PurchaseRequests_PRID",
                        column: x => x.PRID,
                        principalTable: "PurchaseRequests",
                        principalColumn: "PRID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Suppliers_SupplierID",
                        column: x => x.SupplierID,
                        principalTable: "Suppliers",
                        principalColumn: "SupplierID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GoodsReceipts",
                columns: table => new
                {
                    ReceiptID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    POID = table.Column<int>(type: "int", nullable: false),
                    ReceivedByUserID = table.Column<int>(type: "int", nullable: false),
                    DateReceived = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    QuantityReceived = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReceipts", x => x.ReceiptID);
                    table.ForeignKey(
                        name: "FK_GoodsReceipts_PurchaseOrders_POID",
                        column: x => x.POID,
                        principalTable: "PurchaseOrders",
                        principalColumn: "POID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoodsReceipts_Users_ReceivedByUserID",
                        column: x => x.ReceivedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    InvoiceID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    POID = table.Column<int>(type: "int", nullable: false),
                    SupplierID = table.Column<int>(type: "int", nullable: false),
                    PaymentStatusID = table.Column<int>(type: "int", nullable: true),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PaymentDate = table.Column<DateOnly>(type: "date", nullable: true),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AmountDue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PaymentStatus = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.InvoiceID);
                    table.ForeignKey(
                        name: "FK_Invoices_PurchaseOrders_POID",
                        column: x => x.POID,
                        principalTable: "PurchaseOrders",
                        principalColumn: "POID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Invoices_Suppliers_SupplierID",
                        column: x => x.SupplierID,
                        principalTable: "Suppliers",
                        principalColumn: "SupplierID",
                        onDelete: ReferentialAction.Restrict);
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
                name: "SupplierRatings",
                columns: table => new
                {
                    RatingID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    POID = table.Column<int>(type: "int", nullable: false),
                    RatedByUserID = table.Column<int>(type: "int", nullable: false),
                    RatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    RatingScore = table.Column<int>(type: "int", nullable: false),
                    FeedBack = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierRatings", x => x.RatingID);
                    table.ForeignKey(
                        name: "FK_SupplierRatings_PurchaseOrders_POID",
                        column: x => x.POID,
                        principalTable: "PurchaseOrders",
                        principalColumn: "POID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierRatings_Users_RatedByUserID",
                        column: x => x.RatedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
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
                name: "IX_ApprovalHistories_ApproverID",
                table: "ApprovalHistories",
                column: "ApproverID");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalHistories_PRID",
                table: "ApprovalHistories",
                column: "PRID");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRules_MinAmount_MaxAmount_RequiredRoleID_ApprovalLevel",
                table: "ApprovalRules",
                columns: new[] { "MinAmount", "MaxAmount", "RequiredRoleID", "ApprovalLevel" },
                unique: true,
                filter: "[MaxAmount] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRules_RequiredRoleID",
                table: "ApprovalRules",
                column: "RequiredRoleID");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_EntityType_EntityID_FileName",
                table: "Attachments",
                columns: new[] { "EntityType", "EntityID", "FileName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_UploadedByUserID",
                table: "Attachments",
                column: "UploadedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_DepartmentID_FiscalYear",
                table: "Budgets",
                columns: new[] { "DepartmentID", "FiscalYear" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_BudgetCode",
                table: "Departments",
                column: "BudgetCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_ManagerID",
                table: "Departments",
                column: "ManagerID");

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
                name: "IX_GoodsReceipts_POID",
                table: "GoodsReceipts",
                column: "POID");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_ReceivedByUserID",
                table: "GoodsReceipts",
                column: "ReceivedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceNumber_SupplierID",
                table: "Invoices",
                columns: new[] { "InvoiceNumber", "SupplierID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_POID",
                table: "Invoices",
                column: "POID");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_SupplierID",
                table: "Invoices",
                column: "SupplierID");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTerms_Description",
                table: "PaymentTerms",
                column: "Description",
                unique: true);

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
                name: "IX_PRItems_PRID",
                table: "PRItems",
                column: "PRID");

            migrationBuilder.CreateIndex(
                name: "IX_PRItems_ProductID",
                table: "PRItems",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_PRItems_SupplierID",
                table: "PRItems",
                column: "SupplierID");

            migrationBuilder.CreateIndex(
                name: "IX_Products_SupplierID_ProductName",
                table: "Products",
                columns: new[] { "SupplierID", "ProductName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_PRID",
                table: "PurchaseOrders",
                column: "PRID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SupplierID",
                table: "PurchaseOrders",
                column: "SupplierID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequests_RequesterID",
                table: "PurchaseRequests",
                column: "RequesterID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequests_SupplierID",
                table: "PurchaseRequests",
                column: "SupplierID");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionID",
                table: "RolePermissions",
                column: "PermissionID");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_RoleName",
                table: "Roles",
                column: "RoleName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierRatings_POID",
                table: "SupplierRatings",
                column: "POID");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierRatings_RatedByUserID",
                table: "SupplierRatings",
                column: "RatedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_ContactInfo",
                table: "Suppliers",
                column: "ContactInfo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_PaymentTermID",
                table: "Suppliers",
                column: "PaymentTermID");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_SupplierName",
                table: "Suppliers",
                column: "SupplierName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_DepartmentID",
                table: "Users",
                column: "DepartmentID");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleID",
                table: "Users",
                column: "RoleID");

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalHistories_PurchaseRequests_PRID",
                table: "ApprovalHistories",
                column: "PRID",
                principalTable: "PurchaseRequests",
                principalColumn: "PRID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalHistories_Users_ApproverID",
                table: "ApprovalHistories",
                column: "ApproverID",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Attachments_Users_UploadedByUserID",
                table: "Attachments",
                column: "UploadedByUserID",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Budgets_Departments_DepartmentID",
                table: "Budgets",
                column: "DepartmentID",
                principalTable: "Departments",
                principalColumn: "DepartmentID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Users_ManagerID",
                table: "Departments",
                column: "ManagerID",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Users_ManagerID",
                table: "Departments");

            migrationBuilder.DropTable(
                name: "ApprovalHistories");

            migrationBuilder.DropTable(
                name: "ApprovalRules");

            migrationBuilder.DropTable(
                name: "Attachments");

            migrationBuilder.DropTable(
                name: "Budgets");

            migrationBuilder.DropTable(
                name: "GoodsReceiptItems");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "PRItems");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "SupplierRatings");

            migrationBuilder.DropTable(
                name: "GoodsReceipts");

            migrationBuilder.DropTable(
                name: "POItems");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "PurchaseOrders");

            migrationBuilder.DropTable(
                name: "PurchaseRequests");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "PaymentTerms");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
