
using System.Data;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMART_MALL_PARKING_SYSTEM.Data;
using SMART_MALL_PARKING_SYSTEM.Models;

namespace SMART_MALL_PARKING_SYSTEM.Pages.Admin
{
    public class CreditTopUpModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreditTopUpModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string VehicleNumber { get; set; } = "";

        [BindProperty]
        public decimal Amount { get; set; }

        public List<CreditTransaction> RecentTransactions
        { get; set; } = new();

        private bool IsAdmin() =>
            HttpContext.Session.GetString("AdminLoggedIn")
            == "true";

        private static string NormalizePlate(string? plate) =>
            Regex.Replace(
                (plate ?? "").Trim().ToUpperInvariant(),
                @"[\s-]+",
                "");

        public async Task<IActionResult> OnGetAsync()
        {
            if (!IsAdmin())
                return RedirectToPage("/Admin/Login");

            await LoadHistoryAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!IsAdmin())
                return RedirectToPage("/Admin/Login");

            VehicleNumber = NormalizePlate(VehicleNumber);

            if (!Regex.IsMatch(
                VehicleNumber, @"^[A-Z0-9]{2,20}$"))
            {
                ModelState.AddModelError(
                    "", "Enter a valid vehicle number.");
            }

            if (Amount <= 0m || Amount > 10000m ||
                decimal.Round(Amount, 2) != Amount)
            {
                ModelState.AddModelError(
                    "",
                    "Enter an amount from RM0.01 to RM10,000.00, " +
                    "with no more than two decimal places.");
            }

            if (!ModelState.IsValid)
            {
                await LoadHistoryAsync();
                return Page();
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                var users = await _context.Users.ToListAsync();

                var matches = users
                    .Where(u =>
                        NormalizePlate(u.VehicleNumber)
                        == VehicleNumber)
                    .ToList();

                if (matches.Count != 1)
                {
                    ModelState.AddModelError(
                        "",
                        matches.Count == 0
                            ? "No registered user found for this vehicle."
                            : "Multiple users have this vehicle number. " +
                              "Administrator review required.");

                    await transaction.RollbackAsync();
                    await LoadHistoryAsync();
                    return Page();
                }

                var user = matches[0];

                if (user.CreditBalance >
                    decimal.MaxValue - Amount)
                {
                    ModelState.AddModelError(
                        "", "Credit balance limit exceeded.");

                    await transaction.RollbackAsync();
                    await LoadHistoryAsync();
                    return Page();
                }

                user.CreditBalance += Amount;

                var record = new CreditTransaction
                {
                    UserId = user.UserId,
                    Amount = Amount,
                    BalanceAfter = user.CreditBalance,
                    TransactionType = "AdminTopUp",
                    Reference = Guid.NewGuid().ToString("N"),
                    Description =
                        $"Simulated admin top-up for {VehicleNumber}",
                    IsSimulated = true,
                    CreatedAt = DateTime.Now
                };

                _context.CreditTransactions.Add(record);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] =
                    $"Simulated RM{Amount:0.00} credit added " +
                    $"to {VehicleNumber}. " +
                    $"New balance: RM{user.CreditBalance:0.00}.";

                return RedirectToPage();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(
                    "",
                    "Top-up failed. No credit was saved. " +
                    "Check application logs.");
            }

            await LoadHistoryAsync();
            return Page();
        }

        private async Task LoadHistoryAsync()
        {
            RecentTransactions =
                await _context.CreditTransactions
                    .AsNoTracking()
                    .Include(t => t.User)
                    .OrderByDescending(t => t.CreatedAt)
                    .ThenByDescending(t =>
                        t.CreditTransactionId)
                    .Take(30)
                    .ToListAsync();
        }
    }
}
