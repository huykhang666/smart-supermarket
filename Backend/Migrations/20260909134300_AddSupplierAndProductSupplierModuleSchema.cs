using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartSupermarket.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierAndProductSupplierModuleSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "Supplier",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Status",
                table: "Supplier",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)1);

            migrationBuilder.AddColumn<string>(
                name: "SupplierCode",
                table: "Supplier",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TaxCode",
                table: "Supplier",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Supplier",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductSupplier",
                columns: table => new
                {
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    SupplierId = table.Column<int>(type: "integer", nullable: false),
                    PurchasePrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SupplierProductCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LeadTime = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    MinimumOrderQuantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    Rating = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false, defaultValue: 5.00m),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSupplier", x => new { x.ProductId, x.SupplierId });
                    table.ForeignKey(
                        name: "FK_ProductSupplier_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductSupplier_Supplier_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Supplier",
                        principalColumn: "SupplierId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_SupplierCode",
                table: "Supplier",
                column: "SupplierCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_SupplierName",
                table: "Supplier",
                column: "SupplierName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductSupplier_ProductId_IsDefault",
                table: "ProductSupplier",
                columns: new[] { "ProductId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductSupplier_SupplierId",
                table: "ProductSupplier",
                column: "SupplierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductSupplier");

            migrationBuilder.DropIndex(
                name: "IX_Supplier_SupplierCode",
                table: "Supplier");

            migrationBuilder.DropIndex(
                name: "IX_Supplier_SupplierName",
                table: "Supplier");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "Supplier");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Supplier");

            migrationBuilder.DropColumn(
                name: "SupplierCode",
                table: "Supplier");

            migrationBuilder.DropColumn(
                name: "TaxCode",
                table: "Supplier");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Supplier");
        }
    }
}
