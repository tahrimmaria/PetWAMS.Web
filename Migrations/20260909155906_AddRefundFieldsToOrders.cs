using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetWAMS.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundFieldsToOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "RefundAmount",
                table: "PetShopOrders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefundCompletedAt",
                table: "PetShopOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundNote",
                table: "PetShopOrders",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundStatus",
                table: "PetShopOrders",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RefundAmount",
                table: "PetShopOrders");

            migrationBuilder.DropColumn(
                name: "RefundCompletedAt",
                table: "PetShopOrders");

            migrationBuilder.DropColumn(
                name: "RefundNote",
                table: "PetShopOrders");

            migrationBuilder.DropColumn(
                name: "RefundStatus",
                table: "PetShopOrders");
        }
    }
}
