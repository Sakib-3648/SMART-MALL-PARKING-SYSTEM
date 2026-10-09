using System.ComponentModel.DataAnnotations;

namespace SMART_MALL_PARKING_SYSTEM.Models
{
    public class User
    {
        public int UserId { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string VehicleNumber { get; set; }
    }
}