
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMART_MALL_PARKING_SYSTEM.Models
{
    public class ParkingPayment
    {
        [Key]
        public int ParkingPaymentId { get; set; }

        // Connect payment to the vehicle's gate entry.
        [Required]
        public int VehicleEntryId { get; set; }

        [ForeignKey(nameof(VehicleEntryId))]
        public VehicleEntry VehicleEntry { get; set; } = null!;

        // Registered users have a UserId.
        // Guest vehicles can leave this null.
        public int? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        // FPX, DebitCard, CreditCard
        [Required]
        [MaxLength(20)]
        public string PaymentMethod { get; set; } = string.Empty;

        // Pending, Successful, Failed, Cancelled
        [Required]
        [MaxLength(20)]
        public string PaymentStatus { get; set; } = "Pending";

        // Internal payment reference.
        [Required]
        [MaxLength(100)]
        public string TransactionReference { get; set; }
            = Guid.NewGuid().ToString("N");

        // Provider reference, if integrated later.
        [MaxLength(150)]
        public string? ProviderTransactionId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? PaidAt { get; set; }
    }
}
