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

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = HttpContext.Session.GetInt32("UserId");

        if (userId == null)
        {
            return RedirectToPage("/Login");
        }
        Sessions = await _context.ParkingSessions
            .Where(p => p.UserId == userId.Value && p.Status == "Completed")
            .OrderByDescending(p => p.EndTime)
            .ToListAsync();
        return Page();
    }
}