
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

        // Automatic vehicle entry and exit
        public DbSet<VehicleEntry> VehicleEntries { get; set; }

        // FPX and Debit/Credit Card payments
        public DbSet<ParkingPayment> ParkingPayments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Configure parking fees with two decimal places
            modelBuilder.Entity<ParkingSession>()
                .Property(p => p.Fee)
                .HasPrecision(18, 2);

            modelBuilder.Entity<VehicleEntry>()
                .Property(v => v.Fee)
                .HasPrecision(18, 2);
            // Existing parking session relationship
            modelBuilder.Entity<ParkingSession>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Existing vehicle entry relationships
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

            // Existing unique active vehicle entry rule
            modelBuilder.Entity<VehicleEntry>()
                .HasIndex(v => v.VehicleNumber)
                .IsUnique()
                .HasFilter("[ExitTime] IS NULL");

            // Payment belongs to a vehicle entry
            modelBuilder.Entity<ParkingPayment>()
                .HasOne(p => p.VehicleEntry)
                .WithMany()
                .HasForeignKey(p => p.VehicleEntryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Registered user is optional for guest payments
            modelBuilder.Entity<ParkingPayment>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Every payment has a unique transaction reference
            modelBuilder.Entity<ParkingPayment>()
                .HasIndex(p => p.TransactionReference)
                .IsUnique();

            // Existing parking space seed data
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
