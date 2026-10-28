using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetWAMS.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddAdopterVerificationSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdopterVerifications",
                columns: table => new
                {
                    VerificationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    FullNameOnNID = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NIDNumberEncrypted = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    NIDNumberHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    NIDLastFour = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    NIDFrontImagePath = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    NIDBackImagePath = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    VerificationStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AdminRemarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VerifiedByAdminUserID = table.Column<int>(type: "int", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PolicyAccepted = table.Column<bool>(type: "bit", nullable: false),
                    PolicyVersion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PolicyAcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdopterVerifications", x => x.VerificationID);
                    table.ForeignKey(
                        name: "FK_AdopterVerifications_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdopterVerifications_NIDNumberHash",
                table: "AdopterVerifications",
                column: "NIDNumberHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdopterVerifications_UserID",
                table: "AdopterVerifications",
                column: "UserID",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdopterVerifications");
        }
    }
}
