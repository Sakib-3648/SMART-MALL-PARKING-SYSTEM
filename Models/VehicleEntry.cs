
using System;
using System.ComponentModel.DataAnnotations;

namespace SMART_MALL_PARKING_SYSTEM.Models
{
    public class VehicleEntry
    {
        public decimal? Fee { get; set; }

        public int? ParkingSpaceId { get; set; }

        public int VehicleEntryId { get; set; }

        [Required]
        [MaxLength(20)]
        public string VehicleNumber { get; set; } = string.Empty;

        public int? UserId { get; set; }
        public User? User { get; set; }

        public int? ParkingReservationId { get; set; }
        public ParkingReservation? ParkingReservation { get; set; }

        public int? ParkingSessionId { get; set; }
        public ParkingSession? ParkingSession { get; set; }

        public DateTime EntryTime { get; set; }

        public DateTime? ExitTime { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Inside";

        public bool IsSpaceAssigned { get; set; } = false;
    }
}
