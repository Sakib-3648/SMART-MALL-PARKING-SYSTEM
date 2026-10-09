using SMART_MALL_PARKING_SYSTEM.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SMART_MALL_PARKING_SYSTEM.Data;
using SMART_MALL_PARKING_SYSTEM.Models;

namespace SMART_MALL_PARKING_SYSTEM.Pages
{
    public class VerifyEmailModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public VerifyEmailModel(
     ApplicationDbContext context,
     EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [BindProperty]
        public string VerificationCode { get; set; }

        public IActionResult OnGet()
        {
            var pendingEmail =
                HttpContext.Session.GetString("PendingEmail");

            if (string.IsNullOrEmpty(pendingEmail))
            {
                return RedirectToPage("/Register");
            }

            return Page();
        }
        public async Task<IActionResult> OnPostAsync()
        {
            var savedCode =
                HttpContext.Session.GetString("VerificationCode");
            var expiryText =
    HttpContext.Session.GetString("VerificationCodeExpiry");

            var pendingEmail =
                HttpContext.Session.GetString("PendingEmail");
            var pendingFullName =
    HttpContext.Session.GetString("PendingFullName");

            var pendingPassword =
                HttpContext.Session.GetString("PendingPassword");

            var pendingVehicleNumber =
                HttpContext.Session.GetString("PendingVehicleNumber");

            if (string.IsNullOrEmpty(savedCode) ||
                string.IsNullOrEmpty(pendingEmail))
            {
                return RedirectToPage("/Register");
            }
            if (string.IsNullOrEmpty(expiryText) ||
    !DateTime.TryParse(expiryText, out var expiryTime) ||
    DateTime.Now > expiryTime)
            {
                ModelState.AddModelError(
                    "",
                    "Verification code has expired. Please register again.");

                return Page();
            }

            if (VerificationCode != savedCode)
            {
                ModelState.AddModelError(
                    "",
                    "Invalid verification code.");

                return Page();
            }

            var user = new User
            {
                FullName = pendingFullName!,
                Email = pendingEmail,
                Password = pendingPassword!,
                VehicleNumber = pendingVehicleNumber!
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            HttpContext.Session.Remove("PendingFullName");
            HttpContext.Session.Remove("PendingEmail");
            HttpContext.Session.Remove("PendingPassword");
            HttpContext.Session.Remove("PendingVehicleNumber");
            HttpContext.Session.Remove("VerificationCode");
            HttpContext.Session.Remove("VerificationCodeExpiry");

            return RedirectToPage("/Login");
        }
        public async Task<IActionResult> OnPostResendAsync()
        {
            ModelState.Clear();
            var pendingEmail =
                HttpContext.Session.GetString("PendingEmail");

            if (string.IsNullOrEmpty(pendingEmail))
            {
                return RedirectToPage("/Register");
            }

            var newCode =
                Random.Shared.Next(100000, 1000000).ToString();

            HttpContext.Session.SetString(
                "VerificationCode",
                newCode);

            HttpContext.Session.SetString(
                "VerificationCodeExpiry",
                DateTime.Now.AddMinutes(2).ToString("O"));

            await _emailService.SendVerificationCodeAsync(
                pendingEmail,
                newCode);

            TempData["SuccessMessage"] =
     "A new verification code has been sent to your email.";

            return RedirectToPage();
        }
    }
}