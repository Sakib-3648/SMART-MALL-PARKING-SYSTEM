
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMART_MALL_PARKING_SYSTEM.Models
{
    public class CreditTransaction
    {
        public int CreditTransactionId { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BalanceAfter { get; set; }

        [Required]
        [MaxLength(30)]
        public string TransactionType { get; set; } = "";

        [Required]
        [MaxLength(100)]
        public string Reference { get; set; } =
            Guid.NewGuid().ToString("N");

        [MaxLength(250)]
        public string Description { get; set; } = "";

        public int? ParkingReservationId { get; set; }

        public int? VehicleEntryId { get; set; }

        public bool IsSimulated { get; set; }

        public DateTime CreatedAt { get; set; } =
            DateTime.Now;
    }
}
