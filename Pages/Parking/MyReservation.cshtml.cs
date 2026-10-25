
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMART_MALL_PARKING_SYSTEM.Data;
using SMART_MALL_PARKING_SYSTEM.Models;

namespace SMART_MALL_PARKING_SYSTEM.Pages.Parking
{
    public class MyReservationModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public MyReservationModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<ParkingReservation> Reservations { get; set; } = new();

        // Stores the actual space assigned by the vehicle gate.
        // Key = Reservation ID
        // Value = Actual parking space number
        public Dictionary<int, string> ActualAssignedSpaces { get; set; }
            = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToPage("/Login");
            }

            var now = DateTime.Now;

            var expiredReservations = await _context.ParkingReservations
                .Where(r =>
                    r.UserId == userId.Value &&
                    r.Status == "Reserved" &&
                    r.ReservationEnd < now)
                .ToListAsync();

            foreach (var expiredItem in expiredReservations)
            {
                expiredItem.Status = "Expired";
            }

            if (expiredReservations.Count > 0)
            {
                await _context.SaveChangesAsync();
            }

            Reservations = await _context.ParkingReservations
                .Include(r => r.ParkingSpace)
                .Where(r => r.UserId == userId.Value)
                .OrderByDescending(r => r.ReservationStart)
                .ToListAsync();

            // Get vehicle gate records connected to reservations.
            var assignedEntries = await _context.VehicleEntries
                .Where(e =>
                    e.UserId == userId.Value &&
                    e.ParkingReservationId != null &&
                    e.ParkingSpaceId != null &&
                    e.IsSpaceAssigned)
                .OrderByDescending(e => e.EntryTime)
                .Select(e => new
                {
                    ReservationId = e.ParkingReservationId,
                    SpaceId = e.ParkingSpaceId
                })
                .ToListAsync();

            var spaceIds = assignedEntries
                .Where(e => e.SpaceId.HasValue)
                .Select(e => e.SpaceId!.Value)
                .Distinct()
                .ToList();

            var spaceNumbers = await _context.ParkingSpaces
                .Where(p => spaceIds.Contains(p.ParkingSpaceId))
                .ToDictionaryAsync(
                    p => p.ParkingSpaceId,
                    p => p.SpaceNumber);

            foreach (var entry in assignedEntries)
            {
                if (!entry.ReservationId.HasValue ||
                    !entry.SpaceId.HasValue)
                {
                    continue;
                }

                int reservationId = entry.ReservationId.Value;
                int spaceId = entry.SpaceId.Value;

                // The newest gate entry is used for each reservation.
                if (!ActualAssignedSpaces.ContainsKey(reservationId) &&
                    spaceNumbers.TryGetValue(spaceId, out var spaceNumber))
                {
                    ActualAssignedSpaces[reservationId] = spaceNumber;
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostStartParkingAsync(
            int reservationId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToPage("/Login");
            }

            var reservation = await _context.ParkingReservations
                .FirstOrDefaultAsync(r =>
                    r.ParkingReservationId == reservationId &&
                    r.UserId == userId.Value &&
                    r.Status == "Reserved");

            if (reservation == null)
            {
                return RedirectToPage();
            }

            var now = DateTime.Now;

            if (now < reservation.ReservationStart)
            {
                TempData["ErrorMessage"] =
                    "You cannot start parking before the reservation start time.";

                return RedirectToPage();
            }

            if (now > reservation.ReservationEnd)
            {
                TempData["ErrorMessage"] =
                    "This reservation has already expired.";

                return RedirectToPage();
            }

            var existingSession = await _context.ParkingSessions
                .FirstOrDefaultAsync(p =>
                    p.UserId == userId.Value &&
                    p.Status == "Active");

            if (existingSession != null)
            {
                TempData["ErrorMessage"] =
                    "You already have an active parking session.";

                return RedirectToPage("/Parking/MyParking");
            }

            var parkingSpace = await _context.ParkingSpaces
                .FirstOrDefaultAsync(p =>
                    p.ParkingSpaceId == reservation.ParkingSpaceId);

            if (parkingSpace == null ||
                parkingSpace.IsOccupied ||
                parkingSpace.IsBlocked)
            {
                TempData["ErrorMessage"] =
                    "Your reserved parking space is currently unavailable. " +
                    "Please contact the parking administrator.";

                return RedirectToPage();
            }

            var parkingSession = new ParkingSession
            {
                UserId = userId.Value,
                ParkingSpaceId = reservation.ParkingSpaceId,
                StartTime = now,
                Status = "Active",
                Fee = 0
            };

            _context.ParkingSessions.Add(parkingSession);

            parkingSpace.IsOccupied = true;
            reservation.Status = "Started";

            await _context.SaveChangesAsync();

            return RedirectToPage("/Parking/MyParking");
        }

        public async Task<IActionResult> OnPostCancelAsync(
            int reservationId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToPage("/Login");
            }

            var reservation = await _context.ParkingReservations
                .FirstOrDefaultAsync(r =>
                    r.ParkingReservationId == reservationId &&
                    r.UserId == userId.Value);

            if (reservation != null &&
                reservation.Status == "Reserved")
            {
                reservation.Status = "Cancelled";

                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }
    }
}
