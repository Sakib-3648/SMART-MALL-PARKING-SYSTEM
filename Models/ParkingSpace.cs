
using System.ComponentModel.DataAnnotations;

namespace SMART_MALL_PARKING_SYSTEM.Models
{
    public class ParkingSpace
    {
        public int ParkingSpaceId { get; set; }

        [Required]
        public string SpaceNumber { get; set; } = string.Empty;

        [Range(1, 5)]
        public int FloorNumber { get; set; } = 1;

        public bool IsOccupied { get; set; }

        public bool IsBlocked { get; set; }
    }
}
