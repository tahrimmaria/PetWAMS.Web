using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetWAMS.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddMultipleImagesForPetsAndProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PetImages",
                columns: table => new
                {
                    PetImageID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PetID = table.Column<int>(type: "int", nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(350)", maxLength: 350, nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PetImages", x => x.PetImageID);
                    table.ForeignKey(
                        name: "FK_PetImages_Pets_PetID",
                        column: x => x.PetID,
                        principalTable: "Pets",
                        principalColumn: "PetID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PetShopProductImages",
                columns: table => new
                {
                    ProductImageID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductID = table.Column<int>(type: "int", nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(350)", maxLength: 350, nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PetShopProductImages", x => x.ProductImageID);
                    table.ForeignKey(
                        name: "FK_PetShopProductImages_PetShopProducts_ProductID",
                        column: x => x.ProductID,
                        principalTable: "PetShopProducts",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PetImages_PetID",
                table: "PetImages",
                column: "PetID");

            migrationBuilder.CreateIndex(
                name: "IX_PetShopProductImages_ProductID",
                table: "PetShopProductImages",
                column: "ProductID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PetImages");

            migrationBuilder.DropTable(
                name: "PetShopProductImages");
        }
    }
}
