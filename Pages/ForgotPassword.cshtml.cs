using SMART_MALL_PARKING_SYSTEM.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SMART_MALL_PARKING_SYSTEM.Data;
using Microsoft.EntityFrameworkCore;

namespace SMART_MALL_PARKING_SYSTEM.Pages
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public ForgotPasswordModel(
            ApplicationDbContext context,
            EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }
        [BindProperty]
        public string Email { get; set; } = string.Empty;
        public void OnGet()
        {
        }
        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == Email);

            if (user == null)
            {
                ViewData["ErrorMessage"] = "No account found with this email.";
                return Page();
            }

            var verificationCode =
     Random.Shared.Next(100000, 1000000).ToString();

            HttpContext.Session.SetString(
                "ResetPasswordEmail",
                Email);

            HttpContext.Session.SetString(
                "ResetPasswordCode",
                verificationCode);

            HttpContext.Session.SetString(
                "ResetPasswordCodeExpiry",
                DateTime.Now.AddMinutes(2).ToString("O"));

            await _emailService.SendVerificationCodeAsync(
                Email,
                verificationCode);

            return RedirectToPage("/VerifyResetCode");
        }
    }
}
