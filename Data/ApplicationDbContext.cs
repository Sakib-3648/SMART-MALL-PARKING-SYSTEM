using Microsoft.EntityFrameworkCore;
using SMART_MALL_PARKING_SYSTEM.Models;

namespace SMART_MALL_PARKING_SYSTEM.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<ParkingSpace> ParkingSpaces { get; set; }
        public DbSet<ParkingSession> ParkingSessions { get; set; }
        public DbSet<ParkingReservation> ParkingReservations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<ParkingSession>()
    .HasOne(p => p.User)
    .WithMany()
    .HasForeignKey(p => p.UserId)
    .OnDelete(DeleteBehavior.Restrict);

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