
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
        public Dictionary<int, string> RecentSpaceLabels { get; set; }
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
            var reservation = entry.ParkingReservation;

            // Reserve Now: charge for the entire reserved period
            // plus overtime after the scheduled reservation end.
            if (reservation != null &&
                (reservation.ReservationType == "ReserveNow" ||
                 reservation.ReservationType == "Advance"))
            {
                double reservedMinutes =
                    (reservation.ReservationEnd -
                     reservation.ReservationStart).TotalMinutes;

                double overtimeMinutes = Math.Max(
                    0,
                    (now - reservation.ReservationEnd).TotalMinutes);

                // Round billable minutes upward.
                decimal totalMinutes =
                    (decimal)Math.Ceiling(
                        reservedMinutes + overtimeMinutes);

                return Math.Round(
                    totalMinutes * HourlyRate / 60m,
                    2,
                    MidpointRounding.AwayFromZero);
            }

            // Existing calculation for normal parking and
            // Advance Reservation is preserved.
            var billUntil =
                reservation != null &&
                reservation.ReservationEnd > now
                    ? reservation.ReservationEnd
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
    TempData["GatePlate"]?.ToString()
    ?? HttpContext.Session.GetString("LastGatePlate")
    ?? "WAITING";

            DisplaySpace =
                TempData["GateSpace"]?.ToString()
                ?? HttpContext.Session.GetString("LastGateSpace")
                ?? "---";

            DisplayMessage =
                TempData["GateMessage"]?.ToString()
                ?? HttpContext.Session.GetString("LastGateMessage")
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

                string assignedLocation =
    $"Floor {space.FloorNumber} — {space.SpaceNumber}";

                TempData["GatePlate"] = VehicleNumber;
                TempData["GateSpace"] = assignedLocation;
                TempData["GateMessage"] = "ENTRY APPROVED";
                TempData["GateFee"] = null;

                HttpContext.Session.SetString("LastGatePlate", VehicleNumber);
                HttpContext.Session.SetString("LastGateSpace", assignedLocation);
                HttpContext.Session.SetString("LastGateMessage", "ENTRY APPROVED");

                TempData["SuccessMessage"] =
                    $"Vehicle {VehicleNumber} entered. " +
                   $"Assigned space: Floor {space.FloorNumber} — {space.SpaceNumber}." +
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

        // Prepaid amount is taken from the successful reservation credit ledger,
        // not simply from the ReservationType (older reservations may be unpaid).
        private async Task<decimal> GetPrepaidAmountAsync(VehicleEntry entry)
        {
            if (!entry.ParkingReservationId.HasValue || !entry.UserId.HasValue)
                return 0m;

            int reservationId = entry.ParkingReservationId.Value;
            int userId = entry.UserId.Value;
            decimal prepaidDeductions = await _context.CreditTransactions
                .Where(t => t.UserId == userId &&
                            t.ParkingReservationId == reservationId &&
                            (t.TransactionType == "ReserveNow" ||
                             t.TransactionType == "Advance") && t.Amount < 0m)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;
            return -prepaidDeductions;
        }

        private async Task<decimal> GetOutstandingAsync(VehicleEntry entry, DateTime now)
        {
            decimal total = CalculateFee(entry, now);
            decimal prepaid = await GetPrepaidAmountAsync(entry);
            return Math.Max(0m, total - prepaid);
        }

        private void DeductExitCredit(User user, VehicleEntry entry,
            decimal amount, DateTime now)
        {
            if (amount <= 0m) return;
            user.CreditBalance -= amount;
            _context.CreditTransactions.Add(new CreditTransaction
            {
                UserId = user.UserId,
                Amount = -amount,
                BalanceAfter = user.CreditBalance,
                TransactionType = "ExitPayment",
                Reference = $"EXIT-{entry.VehicleEntryId}",
                Description = $"Exit credit payment for {entry.VehicleNumber}",
                VehicleEntryId = entry.VehicleEntryId,
                IsSimulated = true,
                CreatedAt = now
            });
        }

        private async Task CompleteExitAsync(VehicleEntry entry, DateTime now,
            decimal totalFee)
        {
            entry.Fee = totalFee;
            if (entry.ParkingSession != null &&
                entry.ParkingSession.Status == "Active")
            {
                entry.ParkingSession.EndTime = now;
                entry.ParkingSession.Fee = totalFee;
                entry.ParkingSession.Status = "Completed";
            }
            if (entry.ParkingReservation?.Status == "Started")
                entry.ParkingReservation.Status = "Completed";

            if (entry.ParkingSpaceId.HasValue)
            {
                var space = await _context.ParkingSpaces.FindAsync(
                    entry.ParkingSpaceId.Value);
                if (space != null) space.IsOccupied = false;
            }
            entry.ExitTime = now;
            entry.Status = "Exited";
        }

        private void ShowExitCompleted(decimal totalFee, string message)
        {
            TempData["GatePlate"] = VehicleNumber;
            TempData["GateSpace"] = "EXIT";
            TempData["GateMessage"] = "EXIT COMPLETED";
            TempData["GateFee"] = $"RM {totalFee:0.00}";
            TempData["SuccessMessage"] = message;
        }

        // Exit: automatically settle with credit when enough is available.
        // Otherwise display the remaining FPX/card payment requirement.
        public async Task<IActionResult> OnPostExitAsync()
        {
            if (!IsAdmin()) return RedirectToPage("/Admin/Login");
            if (!ValidatePlate()) return Fail("Invalid vehicle number.");

            await using var transaction = await _context.Database
                .BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var entry = await _context.VehicleEntries
                    .Include(v => v.ParkingReservation)
                    .Include(v => v.ParkingSession)
                    .FirstOrDefaultAsync(v => v.VehicleNumber == VehicleNumber &&
                                              v.ExitTime == null);
                if (entry == null)
                    return Fail("No active gate entry found.", "EXIT DENIED");

                bool alreadyPaid = await _context.ParkingPayments.AnyAsync(p =>
                    p.VehicleEntryId == entry.VehicleEntryId &&
                    p.PaymentStatus == "Successful");
                if (alreadyPaid)
                    return Fail("Successful exit payment already exists. Administrator review required.",
                        "PAYMENT REVIEW");

                var now = DateTime.Now;
                decimal totalFee = CalculateFee(entry, now);
                decimal outstanding = await GetOutstandingAsync(entry, now);
                User? user = entry.UserId.HasValue
                    ? await _context.Users.FindAsync(entry.UserId.Value)
                    : null;

                // Zero outstanding (prepaid reservation with no overtime),
                // or wallet covers all remaining charges.
                if (outstanding == 0m ||
                    (user != null && user.CreditBalance >= outstanding))
                {
                    if (outstanding > 0m && user != null)
                        DeductExitCredit(user, entry, outstanding, now);

                    _context.ParkingPayments.Add(new ParkingPayment
                    {
                        VehicleEntryId = entry.VehicleEntryId,
                        UserId = entry.UserId,
                        Amount = outstanding,
                        PaymentMethod = "Wallet",
                        PaymentStatus = "Successful",
                        TransactionReference = Guid.NewGuid().ToString("N"),
                        CreatedAt = now,
                        PaidAt = now
                    });
                    await CompleteExitAsync(entry, now, totalFee);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    ShowExitCompleted(totalFee,
                        $"Credit payment RM {outstanding:0.00} completed. " +
                        "Exit approved and parking space released.");
                    return RedirectToPage();
                }

                // Do not deduct credit yet. Only deduct it if the external
                // simulated payment is approved, in the same transaction.
                decimal walletAvailable = user != null
                    ? Math.Max(0m, user.CreditBalance) : 0m;
                decimal shortfall = Math.Max(0m, outstanding - walletAvailable);
                await transaction.CommitAsync();
                ShowPaymentRequired(VehicleNumber, shortfall);
                TempData["SuccessMessage"] =
                    $"Total fee RM {totalFee:0.00}; prepaid/credit considered. " +
                    $"Available credit RM {walletAvailable:0.00}; " +
                    $"FPX/card shortfall RM {shortfall:0.00}. " +
                    "Credit will be deducted only after successful payment. " +
                    "Gate remains closed.";
                return RedirectToPage();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return Fail("Exit calculation failed. Check application logs.",
                    "EXIT DENIED");
            }
        }

        // Complete a hybrid wallet + simulated FPX/card payment.
        public async Task<IActionResult> OnPostPaymentAsync(
            string paymentMethod, bool approved)
        {
            if (!IsAdmin()) return RedirectToPage("/Admin/Login");
            if (!ValidatePlate()) return Fail("Invalid vehicle number.");
            if (paymentMethod != "FPX" && paymentMethod != "DebitCard" &&
                paymentMethod != "CreditCard")
                return Fail("Invalid payment method.", "PAYMENT DENIED");

            await using var transaction = await _context.Database
                .BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var entry = await _context.VehicleEntries
                    .Include(v => v.ParkingSession)
                    .Include(v => v.ParkingReservation)
                    .FirstOrDefaultAsync(v => v.VehicleNumber == VehicleNumber &&
                                              v.ExitTime == null);
                if (entry == null)
                    return Fail("Vehicle already exited or no active entry found.",
                        "EXIT DENIED");

                bool alreadyPaid = await _context.ParkingPayments.AnyAsync(p =>
                    p.VehicleEntryId == entry.VehicleEntryId &&
                    p.PaymentStatus == "Successful");
                if (alreadyPaid)
                    return Fail("Successful payment already exists. Administrator review required.",
                        "PAYMENT REVIEW");

                var now = DateTime.Now;
                decimal totalFee = CalculateFee(entry, now);
                decimal outstanding = await GetOutstandingAsync(entry, now);
                User? user = entry.UserId.HasValue
                    ? await _context.Users.FindAsync(entry.UserId.Value)
                    : null;
                decimal walletAmount = user != null
                    ? Math.Min(Math.Max(0m, user.CreditBalance), outstanding)
                    : 0m;
                decimal externalAmount = outstanding - walletAmount;

                // A payment button should not be used if wallet can cover all.
                // The normal Exit action settles that case automatically.
                if (externalAmount <= 0m)
                {
                    await transaction.RollbackAsync();
                    return Fail("No FPX/card payment needed. Click Exit to pay with credit.",
                        "USE CREDIT");
                }

                _context.ParkingPayments.Add(new ParkingPayment
                {
                    VehicleEntryId = entry.VehicleEntryId,
                    UserId = entry.UserId,
                    Amount = externalAmount,
                    PaymentMethod = paymentMethod,
                    PaymentStatus = approved ? "Successful" : "Failed",
                    TransactionReference = Guid.NewGuid().ToString("N"),
                    CreatedAt = now,
                    PaidAt = approved ? now : null
                });

                if (!approved)
                {
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    ShowPaymentRequired(VehicleNumber, externalAmount);
                    TempData["GateMessage"] = "PAYMENT FAILED";
                    TempData["ErrorMessage"] =
                        "Simulated payment declined. Credit was not deducted. " +
                        "Exit gate remains closed.";
                    return RedirectToPage();
                }

                if (walletAmount > 0m && user != null)
                    DeductExitCredit(user, entry, walletAmount, now);
                await CompleteExitAsync(entry, now, totalFee);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                ShowExitCompleted(totalFee,
                    $"Simulated {paymentMethod} RM {externalAmount:0.00} " +
                    $"and credit RM {walletAmount:0.00} successful. " +
                    "Exit approved and parking space released.");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return Fail("Payment or exit failed. Check application logs.",
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
            var recentSpaceIds = RecentEntries
    .Where(e => e.ParkingSpaceId.HasValue)
    .Select(e => e.ParkingSpaceId!.Value)
    .Distinct()
    .ToList();

            var recentSpaces = await _context.ParkingSpaces
                .AsNoTracking()
                .Where(p => recentSpaceIds.Contains(p.ParkingSpaceId))
                .ToListAsync();

            RecentSpaceLabels = recentSpaces.ToDictionary(
                p => p.ParkingSpaceId,
                p => $"Floor {p.FloorNumber} — {p.SpaceNumber}");
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
