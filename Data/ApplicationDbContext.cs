
using Microsoft.EntityFrameworkCore;
using SMART_MALL_PARKING_SYSTEM.Models;

namespace SMART_MALL_PARKING_SYSTEM.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<ParkingSpace> ParkingSpaces { get; set; }
        public DbSet<ParkingSession> ParkingSessions { get; set; }
        public DbSet<ParkingReservation> ParkingReservations { get; set; }
        public DbSet<VehicleEntry> VehicleEntries { get; set; }
        public DbSet<ParkingPayment> ParkingPayments { get; set; }

        // New prepaid credit transaction history
        public DbSet<CreditTransaction> CreditTransactions { get; set; }

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Five-floor parking system
            modelBuilder.Entity<ParkingSpace>()
                .Property(p => p.FloorNumber)
                .HasDefaultValue(1);

            modelBuilder.Entity<ParkingSpace>()
                .HasIndex(p => new
                {
                    p.FloorNumber,
                    p.SpaceNumber
                })
                .IsUnique();

            modelBuilder.Entity<User>()
                .Property(u => u.CreditBalance)
                .HasPrecision(18, 2)
                .HasDefaultValue(0m);

            modelBuilder.Entity<CreditTransaction>()
                .Property(t => t.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<CreditTransaction>()
                .Property(t => t.BalanceAfter)
                .HasPrecision(18, 2);

            modelBuilder.Entity<CreditTransaction>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CreditTransaction>()
                .HasIndex(t => t.Reference)
                .IsUnique();

            modelBuilder.Entity<CreditTransaction>()
                .HasIndex(t => new
                {
                    t.UserId,
                    t.CreatedAt
                });

            modelBuilder.Entity<ParkingSession>()
                .Property(p => p.Fee)
                .HasPrecision(18, 2);

            modelBuilder.Entity<VehicleEntry>()
                .Property(v => v.Fee)
                .HasPrecision(18, 2);

            modelBuilder.Entity<ParkingSession>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VehicleEntry>()
                .HasOne(v => v.User)
                .WithMany()
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VehicleEntry>()
                .HasOne(v => v.ParkingReservation)
                .WithMany()
                .HasForeignKey(v => v.ParkingReservationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VehicleEntry>()
                .HasOne(v => v.ParkingSession)
                .WithMany()
                .HasForeignKey(v => v.ParkingSessionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VehicleEntry>()
                .HasIndex(v => v.VehicleNumber)
                .IsUnique()
                .HasFilter("[ExitTime] IS NULL");

            modelBuilder.Entity<ParkingPayment>()
                .HasOne(p => p.VehicleEntry)
                .WithMany()
                .HasForeignKey(p => p.VehicleEntryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ParkingPayment>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ParkingPayment>()
                .HasIndex(p => p.TransactionReference)
                .IsUnique();

            modelBuilder.Entity<ParkingPayment>()
                .HasIndex(
                    p => p.VehicleEntryId,
                    "UX_ParkingPayments_OneSuccessfulPerEntry")
                .IsUnique()
                .HasFilter("[PaymentStatus] = 'Successful'");

            modelBuilder.Entity<ParkingSpace>().HasData(
                new ParkingSpace { ParkingSpaceId = 1, SpaceNumber = "P01", IsOccupied = false },
                new ParkingSpace { ParkingSpaceId = 2, SpaceNumber = "P02", IsOccupied = false },
                new ParkingSpace { ParkingSpaceId = 3, SpaceNumber = "P03", IsOccupied = false },
                new ParkingSpace { ParkingSpaceId = 4, SpaceNumber = "P04", IsOccupied = false },
                new ParkingSpace { ParkingSpaceId = 5, SpaceNumber = "P05", IsOccupied = false },
                new ParkingSpace { ParkingSpaceId = 6, SpaceNumber = "P06", IsOccupied = false },
                new ParkingSpace { ParkingSpaceId = 7, SpaceNumber = "P07", IsOccupied = false },
                new ParkingSpace { ParkingSpaceId = 8, SpaceNumber = "P08", IsOccupied = false },
                new ParkingSpace { ParkingSpaceId = 9, SpaceNumber = "P09", IsOccupied = false },
                new ParkingSpace { ParkingSpaceId = 10, SpaceNumber = "P10", IsOccupied = false }
            );
        }
    }
}
