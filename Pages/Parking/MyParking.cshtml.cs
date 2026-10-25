using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMART_MALL_PARKING_SYSTEM.Data;
using SMART_MALL_PARKING_SYSTEM.Models;

public class MyParkingModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public MyParkingModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public ParkingSession? ActiveSession { get; set; }

    public ParkingSpace? ParkingSpace { get; set; }
    public string? VehicleNumber { get; set; }
    public decimal CurrentFee { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = HttpContext.Session.GetInt32("UserId");

        if (userId == null)
        {
            return RedirectToPage("/Login");
        }
        var user = await _context.Users
    .FirstOrDefaultAsync(u => u.UserId == userId.Value);

        VehicleNumber = user?.VehicleNumber;
        ActiveSession = await _context.ParkingSessions
            .Where(p => p.UserId == userId.Value && p.Status == "Active")
            .OrderByDescending(p => p.StartTime)
            .FirstOrDefaultAsync();

        if (ActiveSession != null)
        {
            ParkingSpace = await _context.ParkingSpaces
                .FirstOrDefaultAsync(p =>
                    p.ParkingSpaceId == ActiveSession.ParkingSpaceId);
            var duration = DateTime.Now - ActiveSession.StartTime;
            var hours = Math.Ceiling(duration.TotalHours);

            if (hours < 1)
            {
                hours = 1;
            }

            CurrentFee = (decimal)hours * 2;
        }
        return Page();
    }

    public async Task<IActionResult> OnPostEndParkingAsync()
    {
        var userId = HttpContext.Session.GetInt32("UserId");

        if (userId == null)
        {
            return RedirectToPage("/Login");
        }

        var activeSession = await _context.ParkingSessions
            .Where(p => p.UserId == userId.Value && p.Status == "Active")
            .OrderByDescending(p => p.StartTime)
            .FirstOrDefaultAsync();

        if (activeSession == null)
        {
            return RedirectToPage("/Parking/Availability");
        }

        var gateControlled = await _context.VehicleEntries
            .AnyAsync(v =>
                v.ParkingSessionId == activeSession.ParkingSessionId &&
                v.ExitTime == null);

        if (gateControlled)
        {
            TempData["ErrorMessage"] =
                "Your vehicle is recorded inside the parking area. " +
                "Parking can only be ended through the exit gate. " +
                "Please contact the administrator if the camera fails.";

            return RedirectToPage("/Parking/MyParking");
        }

        var endTime = DateTime.Now;

        var duration = endTime - activeSession.StartTime;

        var hours = Math.Ceiling(duration.TotalHours);

        if (hours < 1)
        {
            hours = 1;
        }

        activeSession.EndTime = endTime;
        activeSession.Fee = (decimal)hours * 2;
        activeSession.Status = "Completed";

        var parkingSpace = await _context.ParkingSpaces
            .FirstOrDefaultAsync(p =>
                p.ParkingSpaceId == activeSession.ParkingSpaceId);

        if (parkingSpace != null)
        {
            parkingSpace.IsOccupied = false;
        }
        var reservation = await _context.ParkingReservations
    .FirstOrDefaultAsync(r =>
        r.UserId == userId.Value &&
        r.ParkingSpaceId == activeSession.ParkingSpaceId &&
        r.Status == "Started");

        if (reservation != null)
        {
            reservation.Status = "Completed";
        }
        await _context.SaveChangesAsync();

        return RedirectToPage("/Parking/Availability");
    }
}