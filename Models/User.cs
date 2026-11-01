
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMART_MALL_PARKING_SYSTEM.Models
{
    public class User
    {
        public int UserId { get; set; }

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string VehicleNumber { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal CreditBalance { get; set; } = 0m;
    }
}
