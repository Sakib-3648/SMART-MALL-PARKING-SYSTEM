
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMART_MALL_PARKING_SYSTEM.Data;
using SMART_MALL_PARKING_SYSTEM.Models;

public class HistoryModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public HistoryModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<ParkingSession> Sessions { get; set; } = new();

    // ParkingSpaceId -> Floor and Space Number
    public Dictionary<int, string> ParkingSpaceLabels { get; set; }
        = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = HttpContext.Session.GetInt32("UserId");

        if (userId == null)
        {
            return RedirectToPage("/Login");
        }

        // Load completed parking sessions
        Sessions = await _context.ParkingSessions
            .AsNoTracking()
            .Where(p =>
                p.UserId == userId.Value &&
                p.Status == "Completed")
            .OrderByDescending(p => p.EndTime)
            .ToListAsync();

        // Get the actual parking spaces used in these sessions
        var parkingSpaceIds = Sessions
            .Select(s => s.ParkingSpaceId)
            .Distinct()
            .ToList();

        var parkingSpaces = await _context.ParkingSpaces
            .AsNoTracking()
            .Where(p => parkingSpaceIds.Contains(p.ParkingSpaceId))
            .ToListAsync();

        // Format: Floor 3 — P003
        ParkingSpaceLabels = parkingSpaces.ToDictionary(
            p => p.ParkingSpaceId,
            p => $"Floor {p.FloorNumber} — {p.SpaceNumber}"
        );

        return Page();
    }
}
