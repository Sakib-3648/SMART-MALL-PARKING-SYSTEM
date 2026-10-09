using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMART_MALL_PARKING_SYSTEM.Data;

namespace SMART_MALL_PARKING_SYSTEM.Pages
{
    public class IndexModel : PageModel
    {
        public int? UserId { get; set; }
        public string? FullName { get; set; }
        public string? VehicleNumber { get; set; }
        public string? CurrentParkingStatus { get; set; }
        public string? CurrentParkingSpace { get; set; }
        public decimal CurrentFee { get; set; }
        public string? ParkingDuration { get; set; }
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task OnGetAsync()
        {
            UserId = HttpContext.Session.GetInt32("UserId");

            if (UserId != null)
            {
                var user = await _context.Users.FindAsync(UserId.Value);

                if (user != null)
                {
                    FullName = user.FullName;
                    VehicleNumber = user.VehicleNumber;
                }
                var activeSession = await _context.ParkingSessions
    .FirstOrDefaultAsync(p =>
        p.UserId == UserId.Value &&
        p.Status == "Active");
                if (activeSession != null)
                {
                    CurrentParkingStatus = "Active";

                    var parkingSpace = await _context.ParkingSpaces
                        .FindAsync(activeSession.ParkingSpaceId);

                    CurrentParkingSpace = parkingSpace?.SpaceNumber;
                    var duration = DateTime.Now - activeSession.StartTime;
                    ParkingDuration = $"{(int)duration.TotalHours} hours {duration.Minutes} minutes";
                    var hours = Math.Ceiling(duration.TotalHours);

                    if (hours < 1)
                    {
                        hours = 1;
                    }

                    CurrentFee = (decimal)hours * 2;
                }
                else
                {
                    CurrentParkingStatus = "No active parking";
                }
            }
        }
    }
}