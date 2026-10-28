using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetWAMS.Web.Migrations
{
    public partial class AddPetCareShopSystem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PetShopProducts",
                columns: table => new
                {
                    ProductID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),

                    ProductName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),

                    ProductCategory = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),

                    PetType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),

                    OriginalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),

                    DiscountPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),

                    StockQuantity = table.Column<int>(type: "int", nullable: false),

                    LowStockLimit = table.Column<int>(type: "int", nullable: false),

                    ApprovalStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Pending"),

                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),

                    ImagePath = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),

                    Description = table.Column<string>(type: "nvarchar(700)", maxLength: 700, nullable: true),

                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PetShopProducts", x => x.ProductID);
                });

            migrationBuilder.CreateTable(
                name: "PetShopCartItems",
                columns: table => new
                {
                    CartItemID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),

                    UserID = table.Column<int>(type: "int", nullable: false),

                    ProductID = table.Column<int>(type: "int", nullable: false),

                    Quantity = table.Column<int>(type: "int", nullable: false),

                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PetShopCartItems", x => x.CartItemID);

                    table.ForeignKey(
                        name: "FK_PetShopCartItems_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);

                    table.ForeignKey(
                        name: "FK_PetShopCartItems_PetShopProducts_ProductID",
                        column: x => x.ProductID,
                        principalTable: "PetShopProducts",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PetShopOrders",
                columns: table => new
                {
                    OrderID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),

                    UserID = table.Column<int>(type: "int", nullable: false),

                    OrderNumber = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),

                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),

                    PaymentMethod = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false, defaultValue: "Demo Payment"),

                    PaymentStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Paid"),

                    OrderStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Pending"),

                    DeliveryStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Not Shipped"),

                    CancellationRequested = table.Column<bool>(type: "bit", nullable: false),

                    CancellationStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "None"),

                    CancellationReason = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),

                    CustomerName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),

                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),

                    DeliveryAddress = table.Column<string>(type: "nvarchar(350)", maxLength: 350, nullable: true),

                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),

                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),

                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),

                    DeliveredAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PetShopOrders", x => x.OrderID);

                    table.ForeignKey(
                        name: "FK_PetShopOrders_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PetShopOrderItems",
                columns: table => new
                {
                    OrderItemID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),

                    OrderID = table.Column<int>(type: "int", nullable: false),

                    ProductID = table.Column<int>(type: "int", nullable: false),

                    ProductName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),

                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),

                    Quantity = table.Column<int>(type: "int", nullable: false),

                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PetShopOrderItems", x => x.OrderItemID);

                    table.ForeignKey(
                        name: "FK_PetShopOrderItems_PetShopOrders_OrderID",
                        column: x => x.OrderID,
                        principalTable: "PetShopOrders",
                        principalColumn: "OrderID",
                        onDelete: ReferentialAction.Cascade);

                    table.ForeignKey(
                        name: "FK_PetShopOrderItems_PetShopProducts_ProductID",
                        column: x => x.ProductID,
                        principalTable: "PetShopProducts",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PetShopComplaints",
                columns: table => new
                {
                    ComplaintID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),

                    UserID = table.Column<int>(type: "int", nullable: false),

                    OrderID = table.Column<int>(type: "int", nullable: false),

                    ComplaintType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),

                    ComplaintMessage = table.Column<string>(type: "nvarchar(700)", maxLength: 700, nullable: false),

                    ComplaintStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Pending"),

                    AdminReply = table.Column<string>(type: "nvarchar(700)", maxLength: 700, nullable: true),

                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),

                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PetShopComplaints", x => x.ComplaintID);

                    table.ForeignKey(
                        name: "FK_PetShopComplaints_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);

                    table.ForeignKey(
                        name: "FK_PetShopComplaints_PetShopOrders_OrderID",
                        column: x => x.OrderID,
                        principalTable: "PetShopOrders",
                        principalColumn: "OrderID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PetShopCartItems_UserID_ProductID",
                table: "PetShopCartItems",
                columns: new[] { "UserID", "ProductID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PetShopCartItems_ProductID",
                table: "PetShopCartItems",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_PetShopOrders_UserID",
                table: "PetShopOrders",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_PetShopOrderItems_OrderID",
                table: "PetShopOrderItems",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_PetShopOrderItems_ProductID",
                table: "PetShopOrderItems",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_PetShopComplaints_UserID",
                table: "PetShopComplaints",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_PetShopComplaints_OrderID",
                table: "PetShopComplaints",
                column: "OrderID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PetShopComplaints");

            migrationBuilder.DropTable(
                name: "PetShopCartItems");

            migrationBuilder.DropTable(
                name: "PetShopOrderItems");

            migrationBuilder.DropTable(
                name: "PetShopOrders");

            migrationBuilder.DropTable(
                name: "PetShopProducts");
        }
    }
}