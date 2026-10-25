using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMART_MALL_PARKING_SYSTEM.Data;
using SMART_MALL_PARKING_SYSTEM.Models;

namespace SMART_MALL_PARKING_SYSTEM.Pages.Admin
{
    public class ParkingSpacesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ParkingSpacesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<ParkingSpace> ParkingSpaces { get; set; } = new();
        public List<int> ReservedSpaceIds { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (HttpContext.Session.GetString("AdminLoggedIn") != "true")
            {
                return RedirectToPage("/Admin/Login");
            }

            ParkingSpaces = await _context.ParkingSpaces
                .OrderBy(p => p.ParkingSpaceId)
                .ToListAsync();
            var now = DateTime.Now;

            ReservedSpaceIds = await _context.ParkingReservations
                .Where(r =>
                    r.Status == "Reserved" &&
                    r.ReservationEnd >= now)
                .Select(r => r.ParkingSpaceId)
                .Distinct()
                .ToListAsync();
            return Page();
        }
        public async Task<IActionResult> OnPostBlockAsync(int parkingSpaceId)
        {
            if (HttpContext.Session.GetString("AdminLoggedIn") != "true")
            {
                return RedirectToPage("/Admin/Login");
            }

            var space = await _context.ParkingSpaces
                .FirstOrDefaultAsync(p => p.ParkingSpaceId == parkingSpaceId);

            if (space == null)
            {
                return RedirectToPage();
            }

            var isReserved = await _context.ParkingReservations
                .AnyAsync(r =>
                    r.ParkingSpaceId == parkingSpaceId &&
                    r.Status == "Reserved" &&
                    r.ReservationEnd >= DateTime.Now);

            if (!space.IsOccupied && !isReserved)
            {
                space.IsBlocked = true;
                await _context.SaveChangesAsync();
            }

            return RedirectToPage();

        }

        public async Task<IActionResult> OnPostEmergencyBlockAsync(
            int parkingSpaceId)
        {
            if (HttpContext.Session.GetString("AdminLoggedIn") != "true")
            {
                return RedirectToPage("/Admin/Login");
            }

            var space = await _context.ParkingSpaces
                .FirstOrDefaultAsync(p =>
                    p.ParkingSpaceId == parkingSpaceId);

            if (space == null)
            {
                TempData["ErrorMessage"] = "Parking space not found.";
                return RedirectToPage();
            }

            // Emergency block is allowed for reserved spaces,
            // but never for spaces with an active vehicle.
            if (space.IsOccupied)
            {
                TempData["ErrorMessage"] =
                    "Cannot block an occupied space. Clear the vehicle first.";

                return RedirectToPage();
            }

            if (space.IsBlocked)
            {
                TempData["ErrorMessage"] =
                    "This parking space is already blocked.";

                return RedirectToPage();
            }

            space.IsBlocked = true;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Parking space {space.SpaceNumber} emergency blocked. " +
                "Existing reservations have been preserved.";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUnblockAsync(int parkingSpaceId)
        {
            if (HttpContext.Session.GetString("AdminLoggedIn") != "true")
            {
                return RedirectToPage("/Admin/Login");
            }

            var space = await _context.ParkingSpaces
                .FirstOrDefaultAsync(p => p.ParkingSpaceId == parkingSpaceId);

            if (space == null)
            {
                return RedirectToPage();
            }

            space.IsBlocked = false;

            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}