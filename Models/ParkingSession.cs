using System.ComponentModel.DataAnnotations;

namespace SMART_MALL_PARKING_SYSTEM.Models
{
    public class ParkingSession
    {
        public int ParkingSessionId { get; set; }

        public int UserId { get; set; }

        public User? User { get; set; }

        public int ParkingSpaceId { get; set; }

        public ParkingSpace? ParkingSpace { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public decimal Fee { get; set; }

        [Required]
        public string Status { get; set; }
    }
}