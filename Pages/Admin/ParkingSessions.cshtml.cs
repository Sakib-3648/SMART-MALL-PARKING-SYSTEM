
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMART_MALL_PARKING_SYSTEM.Data;
using SMART_MALL_PARKING_SYSTEM.Models;
using System.Data;

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

        // Unregistered vehicles are stored in VehicleEntries.
        public List<VehicleEntry> GuestActiveEntries { get; set; } = new();
        public List<VehicleEntry> GuestParkingHistory { get; set; } = new();

        public Dictionary<int, string> SpaceNumbers { get; set; } = new();

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("AdminLoggedIn") == "true";
        }

        private static decimal CalculateFee(
            DateTime start,
            DateTime end,
            DateTime? reservedEnd = null)
        {
            var billingEnd = reservedEnd.HasValue &&
                             reservedEnd.Value > end
                ? reservedEnd.Value
                : end;

            var hours = Math.Max(
                1,
                Math.Ceiling((billingEnd - start).TotalHours));

            return (decimal)hours * 2m;
        }

        public string GetSpaceNumber(int? parkingSpaceId)
        {
            if (!parkingSpaceId.HasValue)
                return "Not Assigned";

            return SpaceNumbers.TryGetValue(
                parkingSpaceId.Value,
                out var number)
                ? number
                : "Unknown";
        }

        public decimal GetGuestFee(VehicleEntry entry)
        {
            if (entry.Fee.HasValue)
                return entry.Fee.Value;

            // Older completed visits have no saved fee.
            // This is only an estimated fee.
            if (entry.ExitTime.HasValue)
                return CalculateFee(entry.EntryTime, entry.ExitTime.Value);

            return 0m;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!IsAdmin())
                return RedirectToPage("/Admin/Login");

            ActiveSessions = await _context.ParkingSessions
                .AsNoTracking()
                .Include(p => p.User)
                .Include(p => p.ParkingSpace)
                .Where(p => p.Status == "Active")
                .OrderByDescending(p => p.StartTime)
                .ToListAsync();

            ParkingHistory = await _context.ParkingSessions
                .AsNoTracking()
                .Include(p => p.User)
                .Include(p => p.ParkingSpace)
                .Where(p => p.Status == "Completed")
                .OrderByDescending(p => p.EndTime)
                .ToListAsync();

            // Only guest entries without a registered user.
            // This prevents duplicate display of registered sessions.
            GuestActiveEntries = await _context.VehicleEntries
                .AsNoTracking()
                .Where(v => v.UserId == null && v.ExitTime == null)
                .OrderByDescending(v => v.EntryTime)
                .ToListAsync();

            GuestParkingHistory = await _context.VehicleEntries
                .AsNoTracking()
                .Where(v => v.UserId == null && v.ExitTime != null)
                .OrderByDescending(v => v.ExitTime)
                .ToListAsync();

            SpaceNumbers = await _context.ParkingSpaces
                .AsNoTracking()
                .ToDictionaryAsync(
                    p => p.ParkingSpaceId,
                    p => p.SpaceNumber);

            return Page();
        }

        public async Task<IActionResult> OnPostForceEndAsync(
            int parkingSessionId)
        {
            if (!IsAdmin())
                return RedirectToPage("/Admin/Login");

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                var session = await _context.ParkingSessions
                    .FirstOrDefaultAsync(p =>
                        p.ParkingSessionId == parkingSessionId &&
                        p.Status == "Active");

                if (session == null)
                {
                    TempData["ErrorMessage"] =
                        "Active parking session not found.";
                    return RedirectToPage();
                }

                var now = DateTime.Now;

                var gateEntry = await _context.VehicleEntries
                    .Include(v => v.ParkingReservation)
                    .FirstOrDefaultAsync(v =>
                        v.ParkingSessionId == parkingSessionId &&
                        v.ExitTime == null);

                ParkingReservation? reservation = null;

                if (gateEntry?.ParkingReservation != null)
                {
                    reservation = gateEntry.ParkingReservation;
                }
                else
                {
                    reservation = await _context.ParkingReservations
                        .FirstOrDefaultAsync(r =>
                            r.UserId == session.UserId &&
                            r.ParkingSpaceId == session.ParkingSpaceId &&
                            r.Status == "Started");
                }

                var billingStart = gateEntry?.EntryTime
                    ?? session.StartTime;

                var fee = CalculateFee(
                    billingStart,
                    now,
                    reservation?.ReservationEnd);

                session.EndTime = now;
                session.Fee = fee;
                session.Status = "Completed";

                if (reservation != null &&
                    reservation.Status == "Started")
                {
                    reservation.Status = "Completed";
                }

                if (gateEntry != null)
                {
                    gateEntry.ExitTime = now;
                    gateEntry.Status = "Exited";
                    gateEntry.Fee = fee;
                }

                var space = await _context.ParkingSpaces
                    .FirstOrDefaultAsync(p =>
                        p.ParkingSpaceId == session.ParkingSpaceId);

                if (space != null)
                    space.IsOccupied = false;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] =
                    $"Parking session cleared. Fee: RM {fee:0.00} " +
                    "(not yet collected).";
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();

                TempData["ErrorMessage"] =
                    "Could not clear parking. No changes were saved.";
            }

            return RedirectToPage();
        }

        // Emergency admin exit for unregistered vehicles.
        public async Task<IActionResult> OnPostForceGuestExitAsync(
            int vehicleEntryId)
        {
            if (!IsAdmin())
                return RedirectToPage("/Admin/Login");

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                var entry = await _context.VehicleEntries
                    .FirstOrDefaultAsync(v =>
                        v.VehicleEntryId == vehicleEntryId &&
                        v.UserId == null &&
                        v.ExitTime == null);

                if (entry == null)
                {
                    TempData["ErrorMessage"] =
                        "Active guest vehicle entry not found.";
                    return RedirectToPage();
                }

                var now = DateTime.Now;
                var fee = CalculateFee(entry.EntryTime, now);

                entry.ExitTime = now;
                entry.Status = "Exited";
                entry.Fee = fee;

                if (entry.ParkingSpaceId.HasValue)
                {
                    var space = await _context.ParkingSpaces
                        .FirstOrDefaultAsync(p =>
                            p.ParkingSpaceId == entry.ParkingSpaceId.Value);

                    if (space != null)
                        space.IsOccupied = false;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] =
                    $"Guest vehicle {entry.VehicleNumber} cleared. " +
                    $"Fee: RM {fee:0.00} (not yet collected).";
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();

                TempData["ErrorMessage"] =
                    "Could not clear guest vehicle. No changes were saved.";
            }

            return RedirectToPage();
        }
    }
}
