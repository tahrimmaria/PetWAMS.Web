using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetWAMS.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddSslCommerzPaymentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankTransactionID",
                table: "PetShopOrders",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardBrand",
                table: "PetShopOrders",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardIssuer",
                table: "PetShopOrders",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardType",
                table: "PetShopOrders",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GatewayPageURL",
                table: "PetShopOrders",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GatewayStatus",
                table: "PetShopOrders",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentInitiatedAt",
                table: "PetShopOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentValidatedAt",
                table: "PetShopOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RiskLevel",
                table: "PetShopOrders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RiskTitle",
                table: "PetShopOrders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SessionKey",
                table: "PetShopOrders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StoreAmount",
                table: "PetShopOrders",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransactionID",
                table: "PetShopOrders",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValidationID",
                table: "PetShopOrders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankTransactionID",
                table: "PetShopOrders");

            migrationBuilder.DropColumn(
                name: "CardBrand",
                table: "PetShopOrders");

            migrationBuilder.DropColumn(
                name: "CardIssuer",
                table: "PetShopOrders");

            migrationBuilder.DropColumn(
                name: "CardType",
                table: "PetShopOrders");

            migrationBuilder.DropColumn(
                name: "GatewayPageURL",
                table: "PetShopOrders");

            migrationBuilder.DropColumn(
                name: "GatewayStatus",
                table: "PetShopOrders");

            migrationBuilder.DropColumn(
                name: "PaymentInitiatedAt",
                table: "PetShopOrders");

            migrationBuilder.DropColumn(
                name: "PaymentValidatedAt",
                table: "PetShopOrders");

            migrationBuilder.DropColumn(
                name: "RiskLevel",
                table: "PetShopOrders");

            migrationBuilder.DropColumn(
                name: "RiskTitle",
                table: "PetShopOrders");

            migrationBuilder.DropColumn(
                name: "SessionKey",
                table: "PetShopOrders");

            migrationBuilder.DropColumn(
                name: "StoreAmount",
                table: "PetShopOrders");

            migrationBuilder.DropColumn(
                name: "TransactionID",
                table: "PetShopOrders");

            migrationBuilder.DropColumn(
                name: "ValidationID",
                table: "PetShopOrders");
        }
    }
}
