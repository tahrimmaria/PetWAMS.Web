using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetWAMS.Web.Migrations
{
    public partial class AddPetShopProductVariants : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PetShopProductVariants",
                columns: table => new
                {
                    ProductVariantID = table.Column<int>(
                        type: "int",
                        nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),

                    ProductID = table.Column<int>(
                        type: "int",
                        nullable: false),

                    VariantName = table.Column<string>(
                        type: "nvarchar(100)",
                        maxLength: 100,
                        nullable: false),

                    ColorCode = table.Column<string>(
                        type: "nvarchar(30)",
                        maxLength: 30,
                        nullable: true),

                    ImagePath = table.Column<string>(
                        type: "nvarchar(350)",
                        maxLength: 350,
                        nullable: true),

                    StockQuantity = table.Column<int>(
                        type: "int",
                        nullable: false),

                    IsActive = table.Column<bool>(
                        type: "bit",
                        nullable: false),

                    CreatedAt = table.Column<DateTime>(
                        type: "datetime2",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_PetShopProductVariants",
                        x => x.ProductVariantID);

                    table.ForeignKey(
                        name: "FK_PetShopProductVariants_PetShopProducts_ProductID",
                        column: x => x.ProductID,
                        principalTable: "PetShopProducts",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddColumn<int>(
                name: "ProductVariantID",
                table: "PetShopCartItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductVariantID",
                table: "PetShopOrderItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VariantName",
                table: "PetShopOrderItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VariantImagePath",
                table: "PetShopOrderItems",
                type: "nvarchar(350)",
                maxLength: 350,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PetShopProductVariants_ProductID",
                table: "PetShopProductVariants",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_PetShopCartItems_ProductVariantID",
                table: "PetShopCartItems",
                column: "ProductVariantID");

            migrationBuilder.CreateIndex(
                name: "IX_PetShopOrderItems_ProductVariantID",
                table: "PetShopOrderItems",
                column: "ProductVariantID");

            migrationBuilder.AddForeignKey(
                name: "FK_PetShopCartItems_PetShopProductVariants_ProductVariantID",
                table: "PetShopCartItems",
                column: "ProductVariantID",
                principalTable: "PetShopProductVariants",
                principalColumn: "ProductVariantID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PetShopOrderItems_PetShopProductVariants_ProductVariantID",
                table: "PetShopOrderItems",
                column: "ProductVariantID",
                principalTable: "PetShopProductVariants",
                principalColumn: "ProductVariantID",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PetShopCartItems_PetShopProductVariants_ProductVariantID",
                table: "PetShopCartItems");

            migrationBuilder.DropForeignKey(
                name: "FK_PetShopOrderItems_PetShopProductVariants_ProductVariantID",
                table: "PetShopOrderItems");

            migrationBuilder.DropIndex(
                name: "IX_PetShopCartItems_ProductVariantID",
                table: "PetShopCartItems");

            migrationBuilder.DropIndex(
                name: "IX_PetShopOrderItems_ProductVariantID",
                table: "PetShopOrderItems");

            migrationBuilder.DropTable(
                name: "PetShopProductVariants");

            migrationBuilder.DropColumn(
                name: "ProductVariantID",
                table: "PetShopCartItems");

            migrationBuilder.DropColumn(
                name: "ProductVariantID",
                table: "PetShopOrderItems");

            migrationBuilder.DropColumn(
                name: "VariantName",
                table: "PetShopOrderItems");

            migrationBuilder.DropColumn(
                name: "VariantImagePath",
                table: "PetShopOrderItems");
        }
    }
}