using System.ComponentModel.DataAnnotations;

namespace SMART_MALL_PARKING_SYSTEM.Models
{
    public class ParkingSpace
    {
        public int ParkingSpaceId { get; set; }

        [Required]
        public string SpaceNumber { get; set; }

        public bool IsOccupied { get; set; }
        public bool IsBlocked { get; set; }
    }
}