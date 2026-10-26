using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMART_MALL_PARKING_SYSTEM.Migrations
{
    /// <inheritdoc />
    public partial class AddParkingPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParkingPayments",
                columns: table => new
                {
                    ParkingPaymentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleEntryId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PaymentStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TransactionReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProviderTransactionId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingPayments", x => x.ParkingPaymentId);
                    table.ForeignKey(
                        name: "FK_ParkingPayments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParkingPayments_VehicleEntries_VehicleEntryId",
                        column: x => x.VehicleEntryId,
                        principalTable: "VehicleEntries",
                        principalColumn: "VehicleEntryId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParkingPayments_TransactionReference",
                table: "ParkingPayments",
                column: "TransactionReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParkingPayments_UserId",
                table: "ParkingPayments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingPayments_VehicleEntryId",
                table: "ParkingPayments",
                column: "VehicleEntryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParkingPayments");
        }
    }
}
