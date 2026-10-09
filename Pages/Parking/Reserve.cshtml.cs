using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMART_MALL_PARKING_SYSTEM.Data;
using SMART_MALL_PARKING_SYSTEM.Models;

namespace SMART_MALL_PARKING_SYSTEM.Pages.Parking
{
    public class ReserveModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ReserveModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<ParkingSpace> ParkingSpaces { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToPage("/Login");
            }

            ParkingSpaces = await _context.ParkingSpaces
    .Where(p => !p.IsOccupied && !p.IsBlocked)
    .OrderBy(p => p.ParkingSpaceId)
    .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(
            int parkingSpaceId,
            DateTime reservationStart,
            DateTime reservationEnd)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToPage("/Login");
            }

            if (reservationStart <= DateTime.Now)
            {
                ModelState.AddModelError(
                    "",
                    "Reservation start time must be in the future.");
            }

            if (reservationEnd <= reservationStart)
            {
                ModelState.AddModelError(
                    "",
                    "Reservation end time must be after the start time.");
            }

            if (!ModelState.IsValid)
            {
                ParkingSpaces = await _context.ParkingSpaces
   .Where(p => !p.IsOccupied && !p.IsBlocked)
    .OrderBy(p => p.ParkingSpaceId)
    .ToListAsync();

                return Page();
            }
            var selectedSpace = await _context.ParkingSpaces
    .FirstOrDefaultAsync(p => p.ParkingSpaceId == parkingSpaceId);

            if (selectedSpace == null ||
    selectedSpace.IsOccupied ||
    selectedSpace.IsBlocked)
            {
                ModelState.AddModelError("", "This parking space is currently unavailable.");

                ParkingSpaces = await _context.ParkingSpaces
                    .Where(p => !p.IsOccupied)
                    .OrderBy(p => p.ParkingSpaceId)
                    .ToListAsync();

                return Page();
            }
            var conflict = await _context.ParkingReservations
                .AnyAsync(r =>
                    r.ParkingSpaceId == parkingSpaceId &&
                    r.Status == "Reserved" &&
                    reservationStart < r.ReservationEnd &&
                    reservationEnd > r.ReservationStart);

            if (conflict)
            {
                ModelState.AddModelError(
                    "",
                    "This parking space is already reserved for that time.");

                ParkingSpaces = await _context.ParkingSpaces
     .Where(p => !p.IsOccupied && !p.IsBlocked)
     .OrderBy(p => p.ParkingSpaceId)
     .ToListAsync();

                return Page();
            }
            var reservation = new ParkingReservation
            {
                UserId = userId.Value,
                ParkingSpaceId = parkingSpaceId,
                ReservationStart = reservationStart,
                ReservationEnd = reservationEnd,
                Status = "Reserved"
            };

            _context.ParkingReservations.Add(reservation);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Index");

            
        }

    }
}