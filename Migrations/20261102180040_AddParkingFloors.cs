using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMART_MALL_PARKING_SYSTEM.Migrations
{
    /// <inheritdoc />
    public partial class AddParkingFloors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SpaceNumber",
                table: "ParkingSpaces",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "FloorNumber",
                table: "ParkingSpaces",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.UpdateData(
                table: "ParkingSpaces",
                keyColumn: "ParkingSpaceId",
                keyValue: 1,
                column: "FloorNumber",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ParkingSpaces",
                keyColumn: "ParkingSpaceId",
                keyValue: 2,
                column: "FloorNumber",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ParkingSpaces",
                keyColumn: "ParkingSpaceId",
                keyValue: 3,
                column: "FloorNumber",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ParkingSpaces",
                keyColumn: "ParkingSpaceId",
                keyValue: 4,
                column: "FloorNumber",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ParkingSpaces",
                keyColumn: "ParkingSpaceId",
                keyValue: 5,
                column: "FloorNumber",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ParkingSpaces",
                keyColumn: "ParkingSpaceId",
                keyValue: 6,
                column: "FloorNumber",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ParkingSpaces",
                keyColumn: "ParkingSpaceId",
                keyValue: 7,
                column: "FloorNumber",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ParkingSpaces",
                keyColumn: "ParkingSpaceId",
                keyValue: 8,
                column: "FloorNumber",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ParkingSpaces",
                keyColumn: "ParkingSpaceId",
                keyValue: 9,
                column: "FloorNumber",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ParkingSpaces",
                keyColumn: "ParkingSpaceId",
                keyValue: 10,
                column: "FloorNumber",
                value: 1);

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSpaces_FloorNumber_SpaceNumber",
                table: "ParkingSpaces",
                columns: new[] { "FloorNumber", "SpaceNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ParkingSpaces_FloorNumber_SpaceNumber",
                table: "ParkingSpaces");

            migrationBuilder.DropColumn(
                name: "FloorNumber",
                table: "ParkingSpaces");

            migrationBuilder.AlterColumn<string>(
                name: "SpaceNumber",
                table: "ParkingSpaces",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
