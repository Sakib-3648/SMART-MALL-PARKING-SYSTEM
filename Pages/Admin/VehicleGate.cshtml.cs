
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
        private static readonly TimeSpan EarlyWindow =
            TimeSpan.FromMinutes(30);

        public VehicleGateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string VehicleNumber { get; set; } = "";

        public int VehiclesInside { get; set; }
        public int RemainingCapacity { get; set; }

        public List<VehicleEntry> RecentEntries { get; set; }
            = new();
        public Dictionary<int, ParkingPayment> RecentPayments { get; set; }
    = new();

        public string DisplayPlate { get; set; } = "WAITING";
        public string DisplaySpace { get; set; } = "---";
        public string DisplayMessage { get; set; } = "Ready to scan";
        public string DisplayFee { get; set; } = "---";

        // Payment details displayed on the gate page
        public string PaymentPlate { get; set; } = "";
        public string PaymentAmount { get; set; } = "";

        public bool PaymentRequired =>
            !string.IsNullOrWhiteSpace(PaymentPlate);

        private bool IsAdmin() =>
            HttpContext.Session.GetString("AdminLoggedIn") == "true";

        private static string NormalizePlate(string? plate) =>
            Regex.Replace(
                (plate ?? "").Trim().ToUpperInvariant(),
                @"[\s-]+",
                "");

        private bool ValidatePlate()
        {
            VehicleNumber = NormalizePlate(VehicleNumber);

            return Regex.IsMatch(
                VehicleNumber,
                @"^[A-Z0-9]{2,20}$");
        }

        private IActionResult Fail(
            string message,
            string gateMessage = "REQUEST DENIED")
        {
            TempData["ErrorMessage"] = message;
            TempData["GatePlate"] = VehicleNumber;
            TempData["GateSpace"] = "---";
            TempData["GateMessage"] = gateMessage;
            TempData["GateFee"] = null;

            return RedirectToPage();
        }

        private void ShowPaymentRequired(
            string plate,
            decimal amount)
        {
            TempData["GatePlate"] = plate;
            TempData["GateSpace"] = "EXIT";
            TempData["GateMessage"] = "PAYMENT REQUIRED";
            TempData["GateFee"] = $"RM {amount:0.00}";

            TempData["PaymentPlate"] = plate;
            TempData["PaymentAmount"] = $"{amount:0.00}";
        }

        private static decimal CalculateFee(
            VehicleEntry entry,
            DateTime now)
        {
            var billUntil =
                entry.ParkingReservation != null &&
                entry.ParkingReservation.ReservationEnd > now
                    ? entry.ParkingReservation.ReservationEnd
                    : now;

            var hours = Math.Max(
                1,
                Math.Ceiling(
                    (billUntil - entry.EntryTime).TotalHours));

            return (decimal)hours * HourlyRate;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!IsAdmin())
                return RedirectToPage("/Admin/Login");

            DisplayPlate =
                TempData["GatePlate"]?.ToString() ?? "WAITING";

            DisplaySpace =
                TempData["GateSpace"]?.ToString() ?? "---";

            DisplayMessage =
                TempData["GateMessage"]?.ToString()
                ?? "Ready to scan";

            DisplayFee =
                TempData["GateFee"]?.ToString() ?? "---";

            PaymentPlate =
                TempData["PaymentPlate"]?.ToString() ?? "";

            PaymentAmount =
                TempData["PaymentAmount"]?.ToString() ?? "";

            await LoadDataAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostEntryAsync()
        {
            if (!IsAdmin())
                return RedirectToPage("/Admin/Login");

            if (!ValidatePlate())
                return Fail("Invalid vehicle number.");

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                var now = DateTime.Now;

                if (await _context.VehicleEntries.AnyAsync(v =>
                    v.VehicleNumber == VehicleNumber &&
                    v.ExitTime == null))
                {
                    return Fail(
                        "This vehicle is already inside.");
                }

                var users = await _context.Users
                    .AsNoTracking()
                    .Select(u => new
                    {
                        u.UserId,
                        u.VehicleNumber
                    })
                    .ToListAsync();

                var matches = users
                    .Where(u =>
                        NormalizePlate(u.VehicleNumber)
                        == VehicleNumber)
                    .ToList();

                if (matches.Count > 1)
                {
                    return Fail(
                        "Duplicate registered vehicle numbers. " +
                        "Contact administrator.");
                }

                int? userId =
                    matches.Count == 1
                        ? matches[0].UserId
                        : null;

                ParkingSession? session = null;
                ParkingReservation? reservation = null;
                ParkingSpace? space = null;

                if (userId.HasValue)
                {
                    var active =
                        await _context.ParkingSessions
                            .Where(s =>
                                s.UserId == userId.Value &&
                                s.Status == "Active")
                            .ToListAsync();

                    if (active.Count > 1)
                    {
                        return Fail(
                            "Multiple active sessions require " +
                            "administrator review.");
                    }

                    session = active.FirstOrDefault();

                    if (session == null)
                    {
                        var upcoming =
                            await _context.ParkingReservations
                                .Where(r =>
                                    r.UserId == userId.Value &&
                                    r.Status == "Reserved" &&
                                    r.ReservationStart <=
                                        now.Add(EarlyWindow) &&
                                    r.ReservationEnd >= now)
                                .OrderBy(r =>
                                    r.ReservationStart)
                                .ToListAsync();

                        if (upcoming.Count > 1)
                        {
                            return Fail(
                                "Overlapping reservations require " +
                                "administrator review.");
                        }

                        reservation = upcoming.FirstOrDefault();
                    }
                }

                if (session != null)
                {
                    space = await _context.ParkingSpaces
                        .FirstOrDefaultAsync(p =>
                            p.ParkingSpaceId ==
                            session.ParkingSpaceId);

                    if (space == null || space.IsBlocked)
                    {
                        return Fail(
                            "Existing session has an " +
                            "unavailable parking space.");
                    }
                }
                else
                {
                    var reservedIds =
                        await _context.ParkingReservations
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
                                p.ParkingSpaceId ==
                                    reservation.ParkingSpaceId &&
                                !p.IsOccupied &&
                                !p.IsBlocked);
                    }

                    if (space == null)
                    {
                        space = await _context.ParkingSpaces
                            .Where(p =>
                                !p.IsOccupied &&
                                !p.IsBlocked &&
                                !reservedIds.Contains(
                                    p.ParkingSpaceId))
                            .OrderBy(p => p.ParkingSpaceId)
                            .FirstOrDefaultAsync();
                    }

                    if (space == null)
                    {
                        return Fail(
                            "Parking full. No assignable " +
                            "space available.");
                    }

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
                    {
                        reservation.Status = "Started";
                    }
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
                    $"Vehicle {VehicleNumber} entered. " +
                    $"Assigned space: {space.SpaceNumber}." +
                    (reservation != null &&
                     reservation.ParkingSpaceId !=
                         space.ParkingSpaceId
                        ? " Alternative space assigned; " +
                          "original reservation retained."
                        : "");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();

                return Fail(
                    "Entry failed. Check application logs; " +
                    "no entry was saved.");
            }

            return RedirectToPage();
        }

        // EXIT STEP 1:
        // Calculate the fee without completing exit.
        public async Task<IActionResult> OnPostExitAsync()
        {
            if (!IsAdmin())
                return RedirectToPage("/Admin/Login");

            if (!ValidatePlate())
                return Fail("Invalid vehicle number.");

            var entry = await _context.VehicleEntries
                .AsNoTracking()
                .Include(v => v.ParkingReservation)
                .FirstOrDefaultAsync(v =>
                    v.VehicleNumber == VehicleNumber &&
                    v.ExitTime == null);

            if (entry == null)
            {
                return Fail(
                    "No active gate entry found.",
                    "EXIT DENIED");
            }

            var fee = CalculateFee(entry, DateTime.Now);

            ShowPaymentRequired(VehicleNumber, fee);

            TempData["SuccessMessage"] =
                $"Parking fee: RM {fee:0.00}. " +
                "Select a simulated payment method " +
                "to continue. Gate remains closed.";

            return RedirectToPage();
        }

        // EXIT STEP 2:
        // Simulated FPX / debit / credit payment.
        // approved=false simulates a failed payment.
        public async Task<IActionResult> OnPostPaymentAsync(
            string paymentMethod,
            bool approved)
        {
            if (!IsAdmin())
                return RedirectToPage("/Admin/Login");

            if (!ValidatePlate())
                return Fail("Invalid vehicle number.");

            if (paymentMethod != "FPX" &&
                paymentMethod != "DebitCard" &&
                paymentMethod != "CreditCard")
            {
                return Fail(
                    "Invalid payment method.",
                    "PAYMENT DENIED");
            }

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
                {
                    return Fail(
                        "Vehicle already exited or no " +
                        "active entry found.",
                        "EXIT DENIED");
                }

                // Avoid processing another successful payment
                // for the same vehicle entry.
                bool alreadyPaid =
                    await _context.ParkingPayments
                        .AnyAsync(p =>
                            p.VehicleEntryId ==
                                entry.VehicleEntryId &&
                            p.PaymentStatus == "Successful");

                if (alreadyPaid)
                {
                    return Fail(
                        "A successful payment already exists. " +
                        "Administrator review required.",
                        "PAYMENT REVIEW");
                }

                var now = DateTime.Now;

                // Recalculate at payment time because
                // parking duration may have increased.
                decimal fee = CalculateFee(entry, now);

                var payment = new ParkingPayment
                {
                    VehicleEntryId = entry.VehicleEntryId,
                    UserId = entry.UserId,
                    Amount = fee,
                    PaymentMethod = paymentMethod,
                    PaymentStatus =
                        approved ? "Successful" : "Failed",
                    TransactionReference =
                        Guid.NewGuid().ToString("N"),
                    CreatedAt = now,
                    PaidAt = approved ? now : null
                };

                _context.ParkingPayments.Add(payment);

                if (!approved)
                {
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    ShowPaymentRequired(VehicleNumber, fee);

                    TempData["GateMessage"] =
                        "PAYMENT FAILED";

                    TempData["ErrorMessage"] =
                        "Simulated payment declined. " +
                        "The exit gate remains closed. " +
                        "Please try another payment method.";

                    return RedirectToPage();
                }

                // Payment succeeded: complete the exit.
                entry.Fee = fee;

                if (entry.ParkingSession != null &&
                    entry.ParkingSession.Status == "Active")
                {
                    entry.ParkingSession.EndTime = now;
                    entry.ParkingSession.Fee = fee;
                    entry.ParkingSession.Status = "Completed";
                }

                if (entry.ParkingReservation?.Status == "Started")
                {
                    entry.ParkingReservation.Status = "Completed";
                }

                if (entry.ParkingSpaceId.HasValue)
                {
                    var space = await _context.ParkingSpaces
                        .FirstOrDefaultAsync(p =>
                            p.ParkingSpaceId ==
                            entry.ParkingSpaceId.Value);

                    if (space != null)
                    {
                        space.IsOccupied = false;
                    }
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
                    $"Simulated {paymentMethod} payment " +
                    $"successful: RM {fee:0.00}. " +
                    "Exit approved and parking space released.";
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();

                return Fail(
                    "Payment or exit processing failed. " +
                    "Check application logs.",
                    "EXIT DENIED");
            }

            return RedirectToPage();
        }

        private async Task<int> CalculateCapacityAsync()
        {
            var now = DateTime.Now;

            var reservedIds =
                await _context.ParkingReservations
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

            // Find payments belonging to the recent vehicle entries.
            var entryIds = RecentEntries
                .Select(v => v.VehicleEntryId)
                .ToList();

            var payments = await _context.ParkingPayments
                .AsNoTracking()
                .Where(p => entryIds.Contains(p.VehicleEntryId))
                .ToListAsync();

            // Prefer a successful payment over earlier failed attempts.
            // If no successful payment exists, show the latest attempt.
            RecentPayments = payments
                .GroupBy(p => p.VehicleEntryId)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .OrderByDescending(p =>
                            p.PaymentStatus == "Successful")
                        .ThenByDescending(p => p.CreatedAt)
                        .ThenByDescending(p => p.ParkingPaymentId)
                        .First()
                );
        }

    }
}
