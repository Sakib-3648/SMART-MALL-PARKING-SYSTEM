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
            return RedirectToPage("/Login");

        await LoadPageDataAsync();
        return Page();
    }

    private async Task LoadPageDataAsync()
    {
        ParkingSpaces = await _context.ParkingSpaces
            .AsNoTracking()
            .OrderBy(p => p.FloorNumber)
            .ThenBy(p => p.ParkingSpaceId)
            .ToListAsync();

        var now = DateTime.Now;
        var currentReservations = await _context.ParkingReservations
            .AsNoTracking()
            .Where(r => r.Status == "Reserved" &&
                        r.ReservationStart <= now &&
                        r.ReservationEnd > now)
            .Select(r => new { r.ParkingSpaceId, r.ReservationEnd })
            .ToListAsync();

        ReservedSpaceIds = currentReservations
            .Select(r => r.ParkingSpaceId)
            .Distinct()
            .ToList();

        ReservedUntil = currentReservations
            .GroupBy(r => r.ParkingSpaceId)
            .ToDictionary(g => g.Key, g => g.Max(r => r.ReservationEnd));
    }

    // Retained for compatibility with the existing hidden manual selection feature.
    // Normal parking entry continues through the Vehicle Gate.
    public async Task<IActionResult> OnPostSelectAsync(int parkingSpaceId)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return RedirectToPage("/Login");

        var activeSession = await _context.ParkingSessions
            .FirstOrDefaultAsync(p => p.UserId == userId.Value && p.Status == "Active");
        if (activeSession != null)
            return RedirectToPage("/Parking/MyParking");

        var parkingSpace = await _context.ParkingSpaces
            .FirstOrDefaultAsync(p => p.ParkingSpaceId == parkingSpaceId);
        if (parkingSpace == null || parkingSpace.IsOccupied || parkingSpace.IsBlocked)
            return RedirectToPage();

        var now = DateTime.Now;
        var isReservedNow = await _context.ParkingReservations
            .AnyAsync(r => r.ParkingSpaceId == parkingSpaceId &&
                           r.Status == "Reserved" &&
                           r.ReservationStart <= now &&
                           r.ReservationEnd > now);
        if (isReservedNow)
            return RedirectToPage();

        var session = new ParkingSession
        {
            UserId = userId.Value,
            ParkingSpaceId = parkingSpaceId,
            StartTime = now,
            Status = "Active",
            Fee = 0
        };

        _context.ParkingSessions.Add(session);
        parkingSpace.IsOccupied = true;
        await _context.SaveChangesAsync();
        return RedirectToPage("/Parking/MyParking");
    }
}
