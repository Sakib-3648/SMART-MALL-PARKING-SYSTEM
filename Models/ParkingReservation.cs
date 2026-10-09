using System.ComponentModel.DataAnnotations;

namespace SMART_MALL_PARKING_SYSTEM.Models
{
    public class ParkingReservation
    {
        public int ParkingReservationId { get; set; }

        [Required]
        public int UserId { get; set; }
        public User? User { get; set; }

        [Required]
        public int ParkingSpaceId { get; set; }
        public ParkingSpace? ParkingSpace { get; set; }

        [Required]
        public DateTime ReservationStart { get; set; }

        [Required]
        public DateTime ReservationEnd { get; set; }

        public string Status { get; set; } = "Reserved";
    }
} 