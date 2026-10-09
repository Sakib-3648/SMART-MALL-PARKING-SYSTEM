using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using SMART_MALL_PARKING_SYSTEM.Data;

namespace SMART_MALL_PARKING_SYSTEM.Pages.Admin
{
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DashboardModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public int TotalUsers { get; set; }
        public int TotalSpaces { get; set; }
        public int AvailableSpaces { get; set; }
        public int OccupiedSpaces { get; set; }
        public int ReservedSpaces { get; set; }
        public int BlockedSpaces { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (HttpContext.Session.GetString("AdminLoggedIn") != "true")
            {
                return RedirectToPage("/Admin/Login");
            }
            TotalUsers = await _context.Users.CountAsync();
            TotalSpaces = await _context.ParkingSpaces.CountAsync();

            OccupiedSpaces = await _context.ParkingSpaces
                .CountAsync(p => p.IsOccupied);
            BlockedSpaces = await _context.ParkingSpaces
    .CountAsync(p => p.IsBlocked);

            var now = DateTime.Now;

            var reservedSpaceIds = await _context.ParkingReservations
                .Where(r =>
                    r.Status == "Reserved" &&
                    r.ReservationEnd >= now)
                .Select(r => r.ParkingSpaceId)
                .Distinct()
                .ToListAsync();

            ReservedSpaces = await _context.ParkingSpaces
                .CountAsync(p =>
                    !p.IsOccupied &&
                    !p.IsBlocked &&
                    reservedSpaceIds.Contains(p.ParkingSpaceId));

            AvailableSpaces = await _context.ParkingSpaces
                .CountAsync(p =>
                    !p.IsOccupied &&
                    !p.IsBlocked &&
                    !reservedSpaceIds.Contains(p.ParkingSpaceId));
            return Page();
        }
        
    }
}