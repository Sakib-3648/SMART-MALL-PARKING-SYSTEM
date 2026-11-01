
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
        public bool VehicleInside { get; set; }

        private async Task LoadPageDataAsync(int userId)
        {
            ParkingSpaces = await _context.ParkingSpaces
                .Where(p => !p.IsOccupied && !p.IsBlocked)
                .OrderBy(p => p.ParkingSpaceId)
                .ToListAsync();

            VehicleInside = await _context.ParkingSessions
                .AnyAsync(s =>
                    s.UserId == userId &&
                    s.Status == "Active");
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return RedirectToPage("/Login");

            await LoadPageDataAsync(userId.Value);
            return Page();
        }

        // ADVANCE RESERVATION
        public async Task<IActionResult> OnPostAsync(
            int parkingSpaceId,
            DateTime reservationStart,
            DateTime reservationEnd)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return RedirectToPage("/Login");

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
                await LoadPageDataAsync(userId.Value);
                return Page();
            }

            var selectedSpace = await _context.ParkingSpaces
                .FirstOrDefaultAsync(p =>
                    p.ParkingSpaceId == parkingSpaceId);

            if (selectedSpace == null ||
                selectedSpace.IsOccupied ||
                selectedSpace.IsBlocked)
            {
                ModelState.AddModelError(
                    "",
                    "This parking space is currently unavailable.");

                await LoadPageDataAsync(userId.Value);
                return Page();
            }

            bool conflict = await _context.ParkingReservations
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

                await LoadPageDataAsync(userId.Value);
                return Page();
            }

            var reservation = new ParkingReservation
            {
                UserId = userId.Value,
                ParkingSpaceId = parkingSpaceId,
                ReservationStart = reservationStart,
                ReservationEnd = reservationEnd,
                Status = "Reserved",
                ReservationType = "Advance"
            };

            _context.ParkingReservations.Add(reservation);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Index");
        }

        // RESERVE NOW
        public async Task<IActionResult> OnPostReserveNowAsync(
            int parkingSpaceId,
            string duration,
            int customHours = 0,
            int customMinutes = 0)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return RedirectToPage("/Login");

            await LoadPageDataAsync(userId.Value);

            if (VehicleInside)
            {
                ModelState.AddModelError(
                    "",
                    "Your vehicle is already parked. Reserve Now is unavailable.");

                return Page();
            }

            var now = DateTime.Now;

            // Prevent multiple active Reserve Now reservations.
            bool alreadyReserved = await _context.ParkingReservations
                .AnyAsync(r =>
                    r.UserId == userId.Value &&
                    r.ReservationType == "ReserveNow" &&
                    r.Status == "Reserved" &&
                    r.ReservationStart <= now &&
                    r.ReservationEnd > now);

            if (alreadyReserved)
            {
                ModelState.AddModelError(
                    "",
                    "You already have an active Reserve Now reservation. " +
                    "Please wait until it ends before reserving again.");

                return Page();
            }

            DateTime start = DateTime.Now;
            DateTime end;

            switch (duration)
            {
                case "1h":
                    end = start.AddHours(1);
                    break;

                case "2h":
                    end = start.AddHours(2);
                    break;

                case "1d":
                    end = start.AddDays(1);
                    break;

                case "7d":
                    end = start.AddDays(7);
                    break;

                case "1m":
                    end = start.AddMonths(1);
                    break;

                case "custom":
                    if (customHours < 0 ||
                        customHours > 744 ||
                        customMinutes < 0 ||
                        customMinutes > 59 ||
                        (customHours == 0 && customMinutes == 0))
                    {
                        ModelState.AddModelError(
                            "",
                            "Enter a valid custom duration.");

                        return Page();
                    }

                    end = start
                        .AddHours(customHours)
                        .AddMinutes(customMinutes);

                    break;

                default:
                    ModelState.AddModelError(
                        "",
                        "Choose a valid reservation duration.");

                    return Page();
            }

            var selectedSpace = await _context.ParkingSpaces
                .FirstOrDefaultAsync(p =>
                    p.ParkingSpaceId == parkingSpaceId);

            if (selectedSpace == null ||
                selectedSpace.IsOccupied ||
                selectedSpace.IsBlocked)
            {
                ModelState.AddModelError(
                    "",
                    "This parking space is currently unavailable.");

                return Page();
            }

            bool conflict = await _context.ParkingReservations
                .AnyAsync(r =>
                    r.ParkingSpaceId == parkingSpaceId &&
                    r.Status == "Reserved" &&
                    start < r.ReservationEnd &&
                    end > r.ReservationStart);

            if (conflict)
            {
                ModelState.AddModelError(
                    "",
                    "This space is already reserved for the selected period.");

                return Page();
            }

            var reservation = new ParkingReservation
            {
                UserId = userId.Value,
                ParkingSpaceId = parkingSpaceId,
                ReservationStart = start,
                ReservationEnd = end,
                Status = "Reserved",
                ReservationType = "ReserveNow"
            };

            _context.ParkingReservations.Add(reservation);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Index");
        }
    }
}
