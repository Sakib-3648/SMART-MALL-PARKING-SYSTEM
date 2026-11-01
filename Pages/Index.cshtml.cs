
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMART_MALL_PARKING_SYSTEM.Data;
using SMART_MALL_PARKING_SYSTEM.Models;

namespace SMART_MALL_PARKING_SYSTEM.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        private const decimal HourlyRate = 2m;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public int? UserId { get; set; }
        public string? FullName { get; set; }
        public string? VehicleNumber { get; set; }
        public decimal CreditBalance { get; set; }

        public string? CurrentParkingStatus { get; set; }
        public string? CurrentParkingSpace { get; set; }
        public decimal CurrentFee { get; set; }
        public string? ParkingDuration { get; set; }

        // Reservation display properties
        public bool HasActiveReservation { get; set; }
        public DateTime? ReservationStart { get; set; }
        public DateTime? ReservationEnd { get; set; }
        public string? ReservationDuration { get; set; }
        public string? ReservationParkingSpace { get; set; }
        public DateTime? VehicleEnteredAt { get; set; }
        public string? ReservationTimeRemaining { get; set; }

        // Indicates that a started reservation has passed its end time.
        public bool IsReservationOvertime { get; set; }

        public async Task OnGetAsync()
        {
            UserId = HttpContext.Session.GetInt32("UserId");

            if (UserId == null)
                return;

            var user = await _context.Users.FindAsync(UserId.Value);

            if (user != null)
            {
                FullName = user.FullName;
                VehicleNumber = user.VehicleNumber;
                CreditBalance = user.CreditBalance;
            }

            var now = DateTime.Now;

            // Find the existing active parking session.
            var activeSession = await _context.ParkingSessions
                .FirstOrDefaultAsync(p =>
                    p.UserId == UserId.Value &&
                    p.Status == "Active");

            if (activeSession != null)
            {
                CurrentParkingStatus = "Active";

                var parkingSpace = await _context.ParkingSpaces
                    .FindAsync(activeSession.ParkingSpaceId);

                CurrentParkingSpace = parkingSpace?.SpaceNumber;

                var duration = now - activeSession.StartTime;

                ParkingDuration =
                    $"{(int)duration.TotalHours} hours " +
                    $"{duration.Minutes} minutes";

                // Preserve normal parking calculation.
                var hours = Math.Max(
                    1,
                    Math.Ceiling(duration.TotalHours));

                CurrentFee = (decimal)hours * HourlyRate;
            }
            else
            {
                CurrentParkingStatus = "No active parking";
            }

            ParkingReservation? activeReservation = null;

            // When a vehicle is inside, use the reservation
            // actually linked to its active gate entry.
            if (activeSession != null)
            {
                var gateEntry = await _context.VehicleEntries
                    .Include(v => v.ParkingReservation)
                    .Where(v =>
                        v.UserId == UserId.Value &&
                        v.ParkingSessionId ==
                            activeSession.ParkingSessionId &&
                        v.ExitTime == null)
                    .OrderByDescending(v => v.EntryTime)
                    .FirstOrDefaultAsync();

                if (gateEntry != null)
                {
                    activeReservation = gateEntry.ParkingReservation;

                    if (activeReservation != null &&
                        activeReservation.Status != "Started")
                    {
                        activeReservation = null;
                    }
                }
            }

            // If there is no linked started reservation,
            // find a valid reservation that has not expired.
            if (activeReservation == null)
            {
                activeReservation = await _context.ParkingReservations
                    .Where(r =>
                        r.UserId == UserId.Value &&
                        r.Status == "Reserved" &&
                        r.ReservationStart <= now &&
                        r.ReservationEnd > now)
                    .OrderByDescending(r => r.ReservationStart)
                    .FirstOrDefaultAsync();
            }

            if (activeReservation != null)
            {
                HasActiveReservation = true;

                ReservationStart = activeReservation.ReservationStart;
                ReservationEnd = activeReservation.ReservationEnd;

                var reservedDuration =
                    activeReservation.ReservationEnd -
                    activeReservation.ReservationStart;

                ReservationDuration =
                    $"{(int)reservedDuration.TotalHours} hours " +
                    $"{reservedDuration.Minutes} minutes";

                var reservedSpace = await _context.ParkingSpaces
                    .FindAsync(activeReservation.ParkingSpaceId);

                ReservationParkingSpace = reservedSpace?.SpaceNumber;

                if (activeSession != null &&
                    activeReservation.Status == "Started")
                {
                    VehicleEnteredAt = activeSession.StartTime;
                }

                var remaining =
                    activeReservation.ReservationEnd - now;

                if (remaining > TimeSpan.Zero)
                {
                    ReservationTimeRemaining =
                        $"{(int)remaining.TotalHours} hours " +
                        $"{remaining.Minutes} minutes";
                }
                else
                {
                    ReservationTimeRemaining =
                        "Reservation ended — overtime applies";

                    IsReservationOvertime =
                        activeReservation.Status == "Started";
                }

                // Keep the original reservation space display
                // separate from the active parking space.
                if (activeSession == null)
                {
                    CurrentParkingStatus = "Reserved";
                    CurrentParkingSpace =
                        reservedSpace?.SpaceNumber;
                }

                // Reserve Now:
                // Full reserved fee + per-minute overtime.
                if (activeSession != null &&
                    activeReservation.Status == "Started" &&
                    activeReservation.ReservationType == "ReserveNow")
                {
                    double reservedMinutes =
                        reservedDuration.TotalMinutes;

                    double overtimeMinutes = Math.Max(
                        0,
                        (now - activeReservation.ReservationEnd)
                            .TotalMinutes);

                    decimal totalMinutes =
                        (decimal)Math.Ceiling(
                            reservedMinutes + overtimeMinutes);

                    CurrentFee = Math.Round(
                        totalMinutes * HourlyRate / 60m,
                        2,
                        MidpointRounding.AwayFromZero);
                }
            }
        }
    }
}
