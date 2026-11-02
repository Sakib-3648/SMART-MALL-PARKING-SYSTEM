
using System;
using System.Data;
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

        private const decimal HourlyRate = 2m;

        public ReserveModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<ParkingSpace> ParkingSpaces { get; set; } = new();

        public bool VehicleInside { get; set; }

        // Used to open the correct floor and space from Parking Availability.
        [BindProperty(SupportsGet = true)]
        public int? Floor { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? ParkingSpaceId { get; set; }

        private async Task LoadPageDataAsync(int userId)
        {
            ParkingSpaces = await _context.ParkingSpaces
                .Where(p => !p.IsOccupied && !p.IsBlocked)
                .OrderBy(p => p.FloorNumber)
                .ThenBy(p => p.ParkingSpaceId)
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

            await using var transaction = await _context.Database
                .BeginTransactionAsync(IsolationLevel.Serializable);

            try
            {
                // Validate again inside the transaction to protect booking and credit.
                if (reservationStart <= DateTime.Now || reservationEnd <= reservationStart)
                {
                    ModelState.AddModelError("", "Please choose valid future reservation times.");
                    await transaction.RollbackAsync();
                    await LoadPageDataAsync(userId.Value);
                    return Page();
                }

                var selectedSpace = await _context.ParkingSpaces
                    .FirstOrDefaultAsync(p => p.ParkingSpaceId == parkingSpaceId);

                if (selectedSpace == null || selectedSpace.IsOccupied || selectedSpace.IsBlocked)
                {
                    ModelState.AddModelError("", "This parking space is unavailable.");
                    await transaction.RollbackAsync();
                    await LoadPageDataAsync(userId.Value);
                    return Page();
                }

                bool conflict = await _context.ParkingReservations.AnyAsync(r =>
                    r.ParkingSpaceId == parkingSpaceId &&
                    (r.Status == "Reserved" || r.Status == "Started") &&
                    reservationStart < r.ReservationEnd &&
                    reservationEnd > r.ReservationStart);

                if (conflict)
                {
                    ModelState.AddModelError("", "This parking space is already reserved for that time.");
                    await transaction.RollbackAsync();
                    await LoadPageDataAsync(userId.Value);
                    return Page();
                }

                decimal reservedMinutes = (decimal)Math.Ceiling(
                    (reservationEnd - reservationStart).TotalMinutes);
                decimal reservationFee = Math.Round(
                    reservedMinutes * HourlyRate / 60m, 2,
                    MidpointRounding.AwayFromZero);

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == userId.Value);

                if (user == null)
                {
                    ModelState.AddModelError("", "User account not found.");
                    await transaction.RollbackAsync();
                    await LoadPageDataAsync(userId.Value);
                    return Page();
                }

                if (user.CreditBalance < reservationFee)
                {
                    ModelState.AddModelError("",
                        $"Insufficient credit. Required RM{reservationFee:0.00}; " +
                        $"available RM{user.CreditBalance:0.00}.");
                    await transaction.RollbackAsync();
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

                user.CreditBalance -= reservationFee;
                _context.CreditTransactions.Add(new CreditTransaction
                {
                    UserId = user.UserId,
                    Amount = -reservationFee,
                    BalanceAfter = user.CreditBalance,
                    TransactionType = "Advance",
                    Reference = $"ADVANCE-{reservation.ParkingReservationId}",
                    Description = $"Prepaid advance reservation for Floor {selectedSpace.FloorNumber}, {selectedSpace.SpaceNumber}",
                    ParkingReservationId = reservation.ParkingReservationId,
                    IsSimulated = true,
                    CreatedAt = DateTime.Now
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] =
                    $"Advance reservation confirmed. RM{reservationFee:0.00} deducted. " +
                    $"Remaining credit: RM{user.CreditBalance:0.00}.";
                return RedirectToPage("/Index");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Reservation failed. No credit was deducted.");
                await LoadPageDataAsync(userId.Value);
                return Page();
            }
        }

        // RESERVE NOW WITH PREPAID CREDIT
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

            // Full reservation fee, rounded up to billable minutes.
            decimal reservedMinutes =
                (decimal)Math.Ceiling(
                    (end - start).TotalMinutes);

            decimal reservationFee = Math.Round(
                reservedMinutes * HourlyRate / 60m,
                2,
                MidpointRounding.AwayFromZero);

            // Keep the reservation and deduction in one transaction.
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                // Recheck inside the transaction.
                bool vehicleInside = await _context.ParkingSessions
                    .AnyAsync(s =>
                        s.UserId == userId.Value &&
                        s.Status == "Active");

                if (vehicleInside)
                {
                    ModelState.AddModelError(
                        "",
                        "Your vehicle is already parked.");

                    await transaction.RollbackAsync();
                    return Page();
                }

                bool alreadyReserved =
                    await _context.ParkingReservations
                        .AnyAsync(r =>
                            r.UserId == userId.Value &&
                            r.ReservationType == "ReserveNow" &&
                            r.Status == "Reserved" &&
                            r.ReservationStart <= start &&
                            r.ReservationEnd > start);

                if (alreadyReserved)
                {
                    ModelState.AddModelError(
                        "",
                        "You already have an active Reserve Now reservation.");

                    await transaction.RollbackAsync();
                    return Page();
                }

                var selectedSpace =
                    await _context.ParkingSpaces
                        .FirstOrDefaultAsync(p =>
                            p.ParkingSpaceId == parkingSpaceId);

                if (selectedSpace == null ||
                    selectedSpace.IsOccupied ||
                    selectedSpace.IsBlocked)
                {
                    ModelState.AddModelError(
                        "",
                        "This parking space is unavailable.");

                    await transaction.RollbackAsync();
                    return Page();
                }

                bool conflict =
                    await _context.ParkingReservations
                        .AnyAsync(r =>
                            r.ParkingSpaceId == parkingSpaceId &&
                            (r.Status == "Reserved" ||
                             r.Status == "Started") &&
                            start < r.ReservationEnd &&
                            end > r.ReservationStart);

                if (conflict)
                {
                    ModelState.AddModelError(
                        "",
                        "This space is already reserved for that period.");

                    await transaction.RollbackAsync();
                    return Page();
                }

                var user = await _context.Users
                    .FirstOrDefaultAsync(u =>
                        u.UserId == userId.Value);

                if (user == null)
                {
                    ModelState.AddModelError(
                        "",
                        "User account not found.");

                    await transaction.RollbackAsync();
                    return Page();
                }

                if (user.CreditBalance < reservationFee)
                {
                    ModelState.AddModelError(
                        "",
                        $"Insufficient credit. Reservation fee: " +
                        $"RM{reservationFee:0.00}. " +
                        $"Available credit: RM{user.CreditBalance:0.00}. " +
                        "Please request a credit top-up.");

                    await transaction.RollbackAsync();
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

                // Save to obtain the reservation ID.
                await _context.SaveChangesAsync();

                user.CreditBalance -= reservationFee;

                _context.CreditTransactions.Add(
                    new CreditTransaction
                    {
                        UserId = user.UserId,
                        Amount = -reservationFee,
                        BalanceAfter = user.CreditBalance,
                        TransactionType = "ReserveNow",
                        Reference =
                            $"RESERVE-{reservation.ParkingReservationId}",
                        Description =
                            $"Prepaid reservation fee for Floor {selectedSpace.FloorNumber}, " +
                            $"{selectedSpace.SpaceNumber}",
                        ParkingReservationId =
                            reservation.ParkingReservationId,
                        IsSimulated = true,
                        CreatedAt = DateTime.Now
                    });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] =
                    $"Reservation confirmed. " +
                    $"RM{reservationFee:0.00} deducted from credit. " +
                    $"Remaining balance: RM{user.CreditBalance:0.00}.";

                return RedirectToPage("/Index");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(
                    "",
                    "Reservation could not be completed. " +
                    "No credit was deducted. Please try again.");

                await LoadPageDataAsync(userId.Value);
                return Page();
            }
        }
    }
}
