using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMART_MALL_PARKING_SYSTEM.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleEntryTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VehicleEntries",
                columns: table => new
                {
                    VehicleEntryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    ParkingReservationId = table.Column<int>(type: "int", nullable: true),
                    ParkingSessionId = table.Column<int>(type: "int", nullable: true),
                    EntryTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExitTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsSpaceAssigned = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleEntries", x => x.VehicleEntryId);
                    table.ForeignKey(
                        name: "FK_VehicleEntries_ParkingReservations_ParkingReservationId",
                        column: x => x.ParkingReservationId,
                        principalTable: "ParkingReservations",
                        principalColumn: "ParkingReservationId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleEntries_ParkingSessions_ParkingSessionId",
                        column: x => x.ParkingSessionId,
                        principalTable: "ParkingSessions",
                        principalColumn: "ParkingSessionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleEntries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleEntries_ParkingReservationId",
                table: "VehicleEntries",
                column: "ParkingReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleEntries_ParkingSessionId",
                table: "VehicleEntries",
                column: "ParkingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleEntries_UserId",
                table: "VehicleEntries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleEntries_VehicleNumber",
                table: "VehicleEntries",
                column: "VehicleNumber",
                unique: true,
                filter: "[ExitTime] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VehicleEntries");
        }
    }
}
