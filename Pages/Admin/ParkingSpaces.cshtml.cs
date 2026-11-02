
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

        private bool IsAdminLoggedIn()
        {
            return HttpContext.Session.GetString("AdminLoggedIn") == "true";
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!IsAdminLoggedIn())
                return RedirectToPage("/Admin/Login");

            ParkingSpaces = await _context.ParkingSpaces
                .AsNoTracking()
                .OrderBy(p => p.FloorNumber)
                .ThenBy(p => p.ParkingSpaceId)
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

        public async Task<IActionResult> OnPostBlockAsync(
            int parkingSpaceId)
        {
            if (!IsAdminLoggedIn())
                return RedirectToPage("/Admin/Login");

            var space = await _context.ParkingSpaces
                .FirstOrDefaultAsync(p =>
                    p.ParkingSpaceId == parkingSpaceId);

            if (space == null)
            {
                TempData["ErrorMessage"] = "Parking space not found.";
                return RedirectToPage();
            }

            if (space.IsOccupied)
            {
                TempData["ErrorMessage"] =
                    $"Floor {space.FloorNumber} — {space.SpaceNumber} " +
                    "is occupied and cannot be blocked.";

                return RedirectToPage();
            }

            var isReserved = await _context.ParkingReservations
                .AnyAsync(r =>
                    r.ParkingSpaceId == parkingSpaceId &&
                    r.Status == "Reserved" &&
                    r.ReservationEnd >= DateTime.Now);

            if (isReserved)
            {
                TempData["ErrorMessage"] =
                    "This space has a reservation. " +
                    "Use Emergency Block instead.";

                return RedirectToPage();
            }

            space.IsBlocked = true;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Floor {space.FloorNumber} — " +
                $"{space.SpaceNumber} blocked successfully.";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEmergencyBlockAsync(
            int parkingSpaceId)
        {
            if (!IsAdminLoggedIn())
                return RedirectToPage("/Admin/Login");

            var space = await _context.ParkingSpaces
                .FirstOrDefaultAsync(p =>
                    p.ParkingSpaceId == parkingSpaceId);

            if (space == null)
            {
                TempData["ErrorMessage"] = "Parking space not found.";
                return RedirectToPage();
            }

            if (space.IsOccupied)
            {
                TempData["ErrorMessage"] =
                    "Cannot block an occupied space. " +
                    "Clear the vehicle first.";

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
                $"Floor {space.FloorNumber} — " +
                $"{space.SpaceNumber} emergency blocked. " +
                "Existing reservations have been preserved.";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUnblockAsync(
            int parkingSpaceId)
        {
            if (!IsAdminLoggedIn())
                return RedirectToPage("/Admin/Login");

            var space = await _context.ParkingSpaces
                .FirstOrDefaultAsync(p =>
                    p.ParkingSpaceId == parkingSpaceId);

            if (space == null)
            {
                TempData["ErrorMessage"] = "Parking space not found.";
                return RedirectToPage();
            }

            space.IsBlocked = false;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Floor {space.FloorNumber} — " +
                $"{space.SpaceNumber} unblocked successfully.";

            return RedirectToPage();
        }
    }
}
