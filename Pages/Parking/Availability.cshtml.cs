using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMART_MALL_PARKING_SYSTEM.Data;
using SMART_MALL_PARKING_SYSTEM.Models;

public class AvailabilityModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public AvailabilityModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<ParkingSpace> ParkingSpaces { get; set; } = new();
    public List<int> ReservedSpaceIds { get; set; } = new();
    public Dictionary<int, DateTime> ReservedUntil { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = HttpContext.Session.GetInt32("UserId");

        if (userId == null)
        {
            return RedirectToPage("/Login");
        }
        ParkingSpaces = await _context.ParkingSpaces
            .OrderBy(p => p.ParkingSpaceId)
            .ToListAsync();
        var now = DateTime.Now;

        ReservedSpaceIds = await _context.ParkingReservations
            .Where(r =>
                r.Status == "Reserved" &&
               r.ReservationEnd >= now)
            .Select(r => r.ParkingSpaceId)
            .ToListAsync();
        ReservedUntil = await _context.ParkingReservations
    .Where(r =>
        r.Status == "Reserved" &&
        r.ReservationEnd >= now)
    .GroupBy(r => r.ParkingSpaceId)
    .ToDictionaryAsync(
        g => g.Key,
        g => g.Max(r => r.ReservationEnd));
        return Page();
    }

    public async Task<IActionResult> OnPostSelectAsync(int parkingSpaceId)
    {
        var userId = HttpContext.Session.GetInt32("UserId");

        if (userId == null)
        {
            return RedirectToPage("/Login");
        }
        var activeSession = await _context.ParkingSessions
    .FirstOrDefaultAsync(p => p.UserId == userId.Value && p.Status == "Active");

        if (activeSession != null)
        {
            return RedirectToPage("/Parking/MyParking");
        }
        var parkingSpace = await _context.ParkingSpaces
            .FirstOrDefaultAsync(p => p.ParkingSpaceId == parkingSpaceId);

        if (parkingSpace == null ||
     parkingSpace.IsOccupied ||
     parkingSpace.IsBlocked)
        {
            return RedirectToPage();
        }
        var now = DateTime.Now;

        var isReservedNow = await _context.ParkingReservations
            .AnyAsync(r =>
                r.ParkingSpaceId == parkingSpaceId &&
                r.Status == "Reserved" &&
                r.ReservationEnd >= now);

        if (isReservedNow)
        {
            return RedirectToPage();
        }

        var session = new ParkingSession
        {
            UserId = userId.Value,
            ParkingSpaceId = parkingSpaceId,
            StartTime = DateTime.Now,
            Status = "Active",
            Fee = 0
        };

        _context.ParkingSessions.Add(session);

        parkingSpace.IsOccupied = true;

        await _context.SaveChangesAsync();

        return RedirectToPage("/Parking/MyParking");
    }
}