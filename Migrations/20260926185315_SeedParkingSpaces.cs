using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SMART_MALL_PARKING_SYSTEM.Migrations
{
    /// <inheritdoc />
    public partial class SeedParkingSpaces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ParkingSpaces",
                columns: new[] { "ParkingSpaceId", "IsOccupied", "SpaceNumber" },
                values: new object[,]
                {
                    { 1, false, "P01" },
                    { 2, false, "P02" },
                    { 3, false, "P03" },
                    { 4, false, "P04" },
                    { 5, false, "P05" },
                    { 6, false, "P06" },
                    { 7, false, "P07" },
                    { 8, false, "P08" },
                    { 9, false, "P09" },
                    { 10, false, "P10" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ParkingSpaces",
                keyColumn: "ParkingSpaceId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "ParkingSpaces",
                keyColumn: "ParkingSpaceId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "ParkingSpaces",
                keyColumn: "ParkingSpaceId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "ParkingSpaces",
                keyColumn: "ParkingSpaceId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "ParkingSpaces",
                keyColumn: "ParkingSpaceId",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "ParkingSpaces",
                keyColumn: "ParkingSpaceId",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "ParkingSpaces",
                keyColumn: "ParkingSpaceId",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "ParkingSpaces",
                keyColumn: "ParkingSpaceId",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "ParkingSpaces",
                keyColumn: "ParkingSpaceId",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "ParkingSpaces",
                keyColumn: "ParkingSpaceId",
                keyValue: 10);
        }
    }
}
