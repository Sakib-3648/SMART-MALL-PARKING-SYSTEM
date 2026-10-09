using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMART_MALL_PARKING_SYSTEM.Migrations
{
    /// <inheritdoc />
    public partial class AddIsBlockedToParkingSpace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsBlocked",
                table: "ParkingSpaces",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "ParkingSpaces",
                keyColumn: "ParkingSpaceId",
                keyValue: 1,
                column: "IsBlocked",
                value: false);

            migrationBuilder.UpdateData(
                table: "ParkingSpaces",
                keyColumn: "ParkingSpaceId",
                keyValue: 2,
                column: "IsBlocked",
                value: false);

            migrationBuilder.UpdateData(
                table: "ParkingSpaces",
                keyColumn: "ParkingSpaceId",
                keyValue: 3,
                column: "IsBlocked",
                value: false);

            migrationBuilder.UpdateData(
                table: "ParkingSpaces",
                keyColumn: "ParkingSpaceId",
                keyValue: 4,
                column: "IsBlocked",
                value: false);

            migrationBuilder.UpdateData(
                table: "ParkingSpaces",
                keyColumn: "ParkingSpaceId",
                keyValue: 5,
                column: "IsBlocked",
                value: false);

            migrationBuilder.UpdateData(
                table: "ParkingSpaces",
                keyColumn: "ParkingSpaceId",
                keyValue: 6,
                column: "IsBlocked",
                value: false);

            migrationBuilder.UpdateData(
                table: "ParkingSpaces",
                keyColumn: "ParkingSpaceId",
                keyValue: 7,
                column: "IsBlocked",
                value: false);

            migrationBuilder.UpdateData(
                table: "ParkingSpaces",
                keyColumn: "ParkingSpaceId",
                keyValue: 8,
                column: "IsBlocked",
                value: false);

            migrationBuilder.UpdateData(
                table: "ParkingSpaces",
                keyColumn: "ParkingSpaceId",
                keyValue: 9,
                column: "IsBlocked",
                value: false);

            migrationBuilder.UpdateData(
                table: "ParkingSpaces",
                keyColumn: "ParkingSpaceId",
                keyValue: 10,
                column: "IsBlocked",
                value: false);

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSessions_ParkingSpaceId",
                table: "ParkingSessions",
                column: "ParkingSpaceId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSessions_UserId",
                table: "ParkingSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingReservations_ParkingSpaceId",
                table: "ParkingReservations",
                column: "ParkingSpaceId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingReservations_UserId",
                table: "ParkingReservations",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ParkingReservations_ParkingSpaces_ParkingSpaceId",
                table: "ParkingReservations",
                column: "ParkingSpaceId",
                principalTable: "ParkingSpaces",
                principalColumn: "ParkingSpaceId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ParkingReservations_Users_UserId",
                table: "ParkingReservations",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ParkingSessions_ParkingSpaces_ParkingSpaceId",
                table: "ParkingSessions",
                column: "ParkingSpaceId",
                principalTable: "ParkingSpaces",
                principalColumn: "ParkingSpaceId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ParkingSessions_Users_UserId",
                table: "ParkingSessions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParkingReservations_ParkingSpaces_ParkingSpaceId",
                table: "ParkingReservations");

            migrationBuilder.DropForeignKey(
                name: "FK_ParkingReservations_Users_UserId",
                table: "ParkingReservations");

            migrationBuilder.DropForeignKey(
                name: "FK_ParkingSessions_ParkingSpaces_ParkingSpaceId",
                table: "ParkingSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_ParkingSessions_Users_UserId",
                table: "ParkingSessions");

            migrationBuilder.DropIndex(
                name: "IX_ParkingSessions_ParkingSpaceId",
                table: "ParkingSessions");

            migrationBuilder.DropIndex(
                name: "IX_ParkingSessions_UserId",
                table: "ParkingSessions");

            migrationBuilder.DropIndex(
                name: "IX_ParkingReservations_ParkingSpaceId",
                table: "ParkingReservations");

            migrationBuilder.DropIndex(
                name: "IX_ParkingReservations_UserId",
                table: "ParkingReservations");

            migrationBuilder.DropColumn(
                name: "IsBlocked",
                table: "ParkingSpaces");
        }
    }
}
