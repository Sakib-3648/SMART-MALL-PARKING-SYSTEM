
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMART_MALL_PARKING_SYSTEM.Data;
using SMART_MALL_PARKING_SYSTEM.Models;
using System.Data;
using System.Text.RegularExpressions;

namespace SMART_MALL_PARKING_SYSTEM.Pages.Admin
{
    public class VehicleGateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private const decimal HourlyRate = 2m;
        private static readonly TimeSpan EarlyWindow = TimeSpan.FromMinutes(30);

        public VehicleGateModel(ApplicationDbContext context) => _context = context;

        [BindProperty]
        public string VehicleNumber { get; set; } = "";

        public int VehiclesInside { get; set; }
        public int RemainingCapacity { get; set; }
        public List<VehicleEntry> RecentEntries { get; set; } = new();

        public string DisplayPlate { get; set; } = "WAITING";
        public string DisplaySpace { get; set; } = "---";
        public string DisplayMessage { get; set; } = "Ready to scan";

        public string DisplayFee { get; set; } = "---";


        private bool IsAdmin() =>
            HttpContext.Session.GetString("AdminLoggedIn") == "true";

        private static string NormalizePlate(string? plate) =>
            Regex.Replace((plate ?? "").Trim().ToUpperInvariant(), @"[\s-]+", "");

        private bool ValidatePlate()
        {
            VehicleNumber = NormalizePlate(VehicleNumber);
            return Regex.IsMatch(VehicleNumber, @"^[A-Z0-9]{2,20}$");
        }

        private IActionResult Fail(string message)
        {
            TempData["ErrorMessage"] = message;
            TempData["GatePlate"] = VehicleNumber;
            TempData["GateSpace"] = "---";
            TempData["GateMessage"] = "ENTRY DENIED";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!IsAdmin()) return RedirectToPage("/Admin/Login");

            DisplayPlate = TempData["GatePlate"]?.ToString() ?? "WAITING";
            DisplaySpace = TempData["GateSpace"]?.ToString() ?? "---";
            DisplayMessage = TempData["GateMessage"]?.ToString() ?? "Ready to scan";
            DisplayFee = TempData["GateFee"]?.ToString() ?? "---";


            await LoadDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostEntryAsync()
        {
            if (!IsAdmin()) return RedirectToPage("/Admin/Login");
            if (!ValidatePlate()) return Fail("Invalid vehicle number.");

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                var now = DateTime.Now;

                if (await _context.VehicleEntries.AnyAsync(v =>
                    v.VehicleNumber == VehicleNumber && v.ExitTime == null))
                    return Fail("This vehicle is already inside.");

                var users = await _context.Users.AsNoTracking()
                    .Select(u => new { u.UserId, u.VehicleNumber })
                    .ToListAsync();

                var matches = users
                    .Where(u => NormalizePlate(u.VehicleNumber) == VehicleNumber)
                    .ToList();

                if (matches.Count > 1)
                    return Fail("Duplicate registered vehicle numbers. Contact administrator.");

                int? userId = matches.Count == 1 ? matches[0].UserId : null;

                ParkingSession? session = null;
                ParkingReservation? reservation = null;
                ParkingSpace? space = null;

                if (userId.HasValue)
                {
                    var active = await _context.ParkingSessions
                        .Where(s => s.UserId == userId.Value && s.Status == "Active")
                        .ToListAsync();

                    if (active.Count > 1)
                        return Fail("Multiple active sessions require administrator review.");

                    session = active.FirstOrDefault();

                    if (session == null)
                    {
                        var upcoming = await _context.ParkingReservations
                            .Where(r =>
                                r.UserId == userId.Value &&
                                r.Status == "Reserved" &&
                                r.ReservationStart <= now.Add(EarlyWindow) &&
                                r.ReservationEnd >= now)
                            .OrderBy(r => r.ReservationStart)
                            .ToListAsync();

                        if (upcoming.Count > 1)
                            return Fail("Overlapping reservations require administrator review.");

                        reservation = upcoming.FirstOrDefault();
                    }
                }

                if (session != null)
                {
                    space = await _context.ParkingSpaces
                        .FirstOrDefaultAsync(p =>
                            p.ParkingSpaceId == session.ParkingSpaceId);

                    if (space == null || space.IsBlocked)
                        return Fail("Existing session has an unavailable parking space.");
                }
                else
                {
                    // Avoid spaces promised to other reservations.
                    var reservedIds = await _context.ParkingReservations
                        .Where(r =>
                            r.Status == "Reserved" &&
                            r.ReservationEnd >= now)
                        .Select(r => r.ParkingSpaceId)
                        .Distinct()
                        .ToListAsync();

                    if (reservation != null)
                    {
                        space = await _context.ParkingSpaces
                            .FirstOrDefaultAsync(p =>
                                p.ParkingSpaceId == reservation.ParkingSpaceId &&
                                !p.IsOccupied &&
                                !p.IsBlocked);
                    }

                    if (space == null)
                    {
                        space = await _context.ParkingSpaces
                            .Where(p =>
                                !p.IsOccupied &&
                                !p.IsBlocked &&
                                !reservedIds.Contains(p.ParkingSpaceId))
                            .OrderBy(p => p.ParkingSpaceId)
                            .FirstOrDefaultAsync();
                    }

                    if (space == null)
                        return Fail("Parking full. No assignable space available.");

                    space.IsOccupied = true;

                    if (userId.HasValue)
                    {
                        session = new ParkingSession
                        {
                            UserId = userId.Value,
                            ParkingSpaceId = space.ParkingSpaceId,
                            StartTime = now,
                            Status = "Active",
                            Fee = 0m
                        };

                        _context.ParkingSessions.Add(session);
                    }

                    if (reservation != null)
                        reservation.Status = "Started";
                }

                var entry = new VehicleEntry
                {
                    VehicleNumber = VehicleNumber,
                    UserId = userId,
                    ParkingReservation = reservation,
                    ParkingSession = session,
                    ParkingSpaceId = space!.ParkingSpaceId,
                    EntryTime = now,
                    Status = "Inside",
                    IsSpaceAssigned = true
                };

                _context.VehicleEntries.Add(entry);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["GatePlate"] = VehicleNumber;
                TempData["GateSpace"] = space.SpaceNumber;
                TempData["GateMessage"] = "ENTRY APPROVED";
                TempData["GateFee"] = null;

                TempData["SuccessMessage"] =
                    $"Vehicle {VehicleNumber} entered. Assigned space: {space.SpaceNumber}." +
                    (reservation != null &&
                     reservation.ParkingSpaceId != space.ParkingSpaceId
                        ? " Alternative space assigned; original reservation space retained in reservation record."
                        : "");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return Fail("Entry failed. Check application logs; no entry was saved.");
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostExitAsync()
        {
            if (!IsAdmin()) return RedirectToPage("/Admin/Login");
            if (!ValidatePlate()) return Fail("Invalid vehicle number.");

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                var entry = await _context.VehicleEntries
                    .Include(v => v.ParkingSession)
                    .Include(v => v.ParkingReservation)
                    .FirstOrDefaultAsync(v =>
                        v.VehicleNumber == VehicleNumber &&
                        v.ExitTime == null);

                if (entry == null)
                    return Fail("No active gate entry found for this vehicle.");

                var now = DateTime.Now;

                var billUntil =
                    entry.ParkingReservation != null &&
                    entry.ParkingReservation.ReservationEnd > now
                        ? entry.ParkingReservation.ReservationEnd
                        : now;

                var hours = Math.Max(
                    1,
                    Math.Ceiling((billUntil - entry.EntryTime).TotalHours));

                var fee = (decimal)hours * HourlyRate;
                entry.Fee = fee;

                if (entry.ParkingSession != null &&
                    entry.ParkingSession.Status == "Active")
                {
                    entry.ParkingSession.EndTime = now;
                    entry.ParkingSession.Fee = fee;
                    entry.ParkingSession.Status = "Completed";
                }

                if (entry.ParkingReservation?.Status == "Started")
                    entry.ParkingReservation.Status = "Completed";

                if (entry.ParkingSpaceId.HasValue)
                {
                    var space = await _context.ParkingSpaces
                        .FirstOrDefaultAsync(p =>
                            p.ParkingSpaceId == entry.ParkingSpaceId.Value);

                    if (space != null)
                        space.IsOccupied = false;
                }

                entry.ExitTime = now;
                entry.Status = "Exited";

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["GatePlate"] = VehicleNumber;
                TempData["GateSpace"] = "EXIT";
                TempData["GateMessage"] = "EXIT COMPLETED";
                TempData["GateFee"] = $"RM {fee:0.00}";


                TempData["SuccessMessage"] =
                    $"Vehicle {VehicleNumber} exited. Fee: RM {fee:0.00} (not yet collected).";
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return Fail("Exit failed. Check application logs.");
            }

            return RedirectToPage();
        }

        private async Task<int> CalculateCapacityAsync()
        {
            var now = DateTime.Now;

            var reservedIds = await _context.ParkingReservations
                .Where(r =>
                    r.Status == "Reserved" &&
                    r.ReservationEnd >= now)
                .Select(r => r.ParkingSpaceId)
                .Distinct()
                .ToListAsync();

            return await _context.ParkingSpaces.CountAsync(p =>
                !p.IsOccupied &&
                !p.IsBlocked &&
                !reservedIds.Contains(p.ParkingSpaceId));
        }

        private async Task LoadDataAsync()
        {
            VehiclesInside = await _context.VehicleEntries
                .CountAsync(v => v.ExitTime == null);

            RemainingCapacity = await CalculateCapacityAsync();

            RecentEntries = await _context.VehicleEntries
                .AsNoTracking()
                .OrderByDescending(v => v.EntryTime)
                .Take(20)
                .ToListAsync();
        }
    }
}
