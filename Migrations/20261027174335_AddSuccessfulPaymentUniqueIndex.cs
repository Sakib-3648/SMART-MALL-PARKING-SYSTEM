
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMART_MALL_PARKING_SYSTEM.Migrations
{
    public partial class AddSuccessfulPaymentUniqueIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove the old non-unique index if it exists.
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_ParkingPayments_VehicleEntryId'
                      AND object_id = OBJECT_ID('dbo.ParkingPayments')
                )
                BEGIN
                    DROP INDEX IX_ParkingPayments_VehicleEntryId
                    ON dbo.ParkingPayments;
                END
            ");

            // Create the unique filtered index only if missing.
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'UX_ParkingPayments_OneSuccessfulPerEntry'
                      AND object_id = OBJECT_ID('dbo.ParkingPayments')
                )
                BEGIN
                    CREATE UNIQUE INDEX
                        UX_ParkingPayments_OneSuccessfulPerEntry
                    ON dbo.ParkingPayments (VehicleEntryId)
                    WHERE PaymentStatus = 'Successful';
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'UX_ParkingPayments_OneSuccessfulPerEntry'
                      AND object_id = OBJECT_ID('dbo.ParkingPayments')
                )
                BEGIN
                    DROP INDEX UX_ParkingPayments_OneSuccessfulPerEntry
                    ON dbo.ParkingPayments;
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_ParkingPayments_VehicleEntryId'
                      AND object_id = OBJECT_ID('dbo.ParkingPayments')
                )
                BEGIN
                    CREATE INDEX IX_ParkingPayments_VehicleEntryId
                    ON dbo.ParkingPayments (VehicleEntryId);
                END
            ");
        }
    }
}
