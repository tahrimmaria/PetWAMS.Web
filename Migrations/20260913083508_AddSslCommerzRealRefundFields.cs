using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetWAMS.Web.Migrations
{
    public partial class AddSslCommerzRealRefundFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // -----------------------------------------------------
            // SAFE / NON-DESTRUCTIVE MIGRATION
            // Adds a column only when it does not already exist.
            // -----------------------------------------------------

            migrationBuilder.Sql(@"
                IF COL_LENGTH('PetShopOrders', 'RefundTransactionID') IS NULL
                BEGIN
                    ALTER TABLE [PetShopOrders]
                    ADD [RefundTransactionID] nvarchar(60) NULL;
                END
            ");

            migrationBuilder.Sql(@"
                IF COL_LENGTH('PetShopOrders', 'RefundReferenceID') IS NULL
                BEGIN
                    ALTER TABLE [PetShopOrders]
                    ADD [RefundReferenceID] nvarchar(100) NULL;
                END
            ");

            migrationBuilder.Sql(@"
                IF COL_LENGTH('PetShopOrders', 'RefundGatewayStatus') IS NULL
                BEGIN
                    ALTER TABLE [PetShopOrders]
                    ADD [RefundGatewayStatus] nvarchar(30) NULL;
                END
            ");

            migrationBuilder.Sql(@"
                IF COL_LENGTH('PetShopOrders', 'RefundGatewayMessage') IS NULL
                BEGIN
                    ALTER TABLE [PetShopOrders]
                    ADD [RefundGatewayMessage] nvarchar(500) NULL;
                END
            ");

            migrationBuilder.Sql(@"
                IF COL_LENGTH('PetShopOrders', 'RefundInitiatedAt') IS NULL
                BEGIN
                    ALTER TABLE [PetShopOrders]
                    ADD [RefundInitiatedAt] datetime2 NULL;
                END
            ");

            migrationBuilder.Sql(@"
                IF COL_LENGTH('PetShopOrders', 'RefundLastCheckedAt') IS NULL
                BEGIN
                    ALTER TABLE [PetShopOrders]
                    ADD [RefundLastCheckedAt] datetime2 NULL;
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            
        }
    }
}