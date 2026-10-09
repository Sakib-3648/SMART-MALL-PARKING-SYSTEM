using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMART_MALL_PARKING_SYSTEM.Data;
using SMART_MALL_PARKING_SYSTEM.Models;

namespace SMART_MALL_PARKING_SYSTEM.Pages.Admin
{
    public class ParkingSessionsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ParkingSessionsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<ParkingSession> ActiveSessions { get; set; } = new();
        public List<ParkingSession> ParkingHistory { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (HttpContext.Session.GetString("AdminLoggedIn") != "true")
            {
                return RedirectToPage("/Admin/Login");
            }

            ActiveSessions = await _context.ParkingSessions
                .Include(p => p.User)
.Include(p => p.ParkingSpace)
                .Where(p => p.Status == "Active")
                .OrderByDescending(p => p.StartTime)
                .ToListAsync();

            ParkingHistory = await _context.ParkingSessions
                .Include(p => p.User)
                .Include(p => p.ParkingSpace)
                .Where(p => p.Status == "Completed")
                .OrderByDescending(p => p.EndTime)
                .ToListAsync();

            return Page();

        }
        public async Task<IActionResult> OnPostForceEndAsync(int parkingSessionId)
        {
            if (HttpContext.Session.GetString("AdminLoggedIn") != "true")
            {
                return RedirectToPage("/Admin/Login");
            }

            var session = await _context.ParkingSessions
                .FirstOrDefaultAsync(p =>
                    p.ParkingSessionId == parkingSessionId &&
                    p.Status == "Active");

            if (session == null)
            {
                return RedirectToPage();
            }

            session.EndTime = DateTime.Now;

            var totalHours = (session.EndTime.Value - session.StartTime).TotalHours;
            var chargedHours = Math.Max(1, Math.Ceiling(totalHours));

            session.Fee = (decimal)chargedHours * 2;

            session.Status = "Completed";

            var parkingSpace = await _context.ParkingSpaces
                .FirstOrDefaultAsync(p =>
                    p.ParkingSpaceId == session.ParkingSpaceId);

            if (parkingSpace != null)
            {
                parkingSpace.IsOccupied = false;
            }
            var reservation = await _context.ParkingReservations
    .FirstOrDefaultAsync(r =>
        r.UserId == session.UserId &&
        r.ParkingSpaceId == session.ParkingSpaceId &&
        r.Status == "Started");

            if (reservation != null)
            {
                reservation.Status = "Completed";
            }
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}