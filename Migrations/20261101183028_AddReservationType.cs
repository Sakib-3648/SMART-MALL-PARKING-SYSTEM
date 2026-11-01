using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMART_MALL_PARKING_SYSTEM.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReservationType",
                table: "ParkingReservations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReservationType",
                table: "ParkingReservations");
        }
    }
}
