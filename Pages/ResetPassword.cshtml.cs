using Microsoft.AspNetCore.Identity;
using SMART_MALL_PARKING_SYSTEM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMART_MALL_PARKING_SYSTEM.Data;

namespace SMART_MALL_PARKING_SYSTEM.Pages
{
    public class ResetPasswordModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher;

        public ResetPasswordModel(ApplicationDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
        }

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string NewPassword { get; set; } = string.Empty;

        [BindProperty]
        public string ConfirmPassword { get; set; } = string.Empty;

        public IActionResult OnGet(string email)
        {
            var isVerified =
                HttpContext.Session.GetString("ResetPasswordVerified");

            var resetEmail =
                HttpContext.Session.GetString("ResetPasswordEmail");

            if (isVerified != "true" ||
                string.IsNullOrEmpty(resetEmail))
            {
                return RedirectToPage("/ForgotPassword");
            }

            Email = resetEmail;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var isVerified =
    HttpContext.Session.GetString("ResetPasswordVerified");

            var resetEmail =
                HttpContext.Session.GetString("ResetPasswordEmail");

            if (isVerified != "true" ||
                string.IsNullOrEmpty(resetEmail))
            {
                return RedirectToPage("/ForgotPassword");
            }

            Email = resetEmail;
            if (NewPassword != ConfirmPassword)
            {
                ViewData["ErrorMessage"] = "Passwords do not match.";
                return Page();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == Email);

            if (user == null)
            {
                ViewData["ErrorMessage"] = "User not found.";
                return Page();
            }

            user.Password =
      _passwordHasher.HashPassword(user, NewPassword);

            await _context.SaveChangesAsync();
            HttpContext.Session.Remove("ResetPasswordEmail");
            HttpContext.Session.Remove("ResetPasswordCode");
            HttpContext.Session.Remove("ResetPasswordCodeExpiry");
            HttpContext.Session.Remove("ResetPasswordVerified");
            return RedirectToPage("/Login");
        }
    }
}