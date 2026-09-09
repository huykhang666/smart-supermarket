using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartSupermarket.Backend.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCategoryModuleSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Category",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrderIndex",
                table: "Category",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "Category",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Category",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte>(
                name: "Status",
                table: "Category",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)1);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Category",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Category_CategoryName",
                table: "Category",
                column: "CategoryName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Category_ParentId",
                table: "Category",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Category_Slug",
                table: "Category",
                column: "Slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Category_Category_ParentId",
                table: "Category",
                column: "ParentId",
                principalTable: "Category",
                principalColumn: "CategoryId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Category_Category_ParentId",
                table: "Category");

            migrationBuilder.DropIndex(
                name: "IX_Category_CategoryName",
                table: "Category");

            migrationBuilder.DropIndex(
                name: "IX_Category_ParentId",
                table: "Category");

            migrationBuilder.DropIndex(
                name: "IX_Category_Slug",
                table: "Category");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Category");

            migrationBuilder.DropColumn(
                name: "OrderIndex",
                table: "Category");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "Category");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Category");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Category");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Category");
        }
    }
}
