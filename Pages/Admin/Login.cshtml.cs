using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SMART_MALL_PARKING_SYSTEM.Pages.Admin
{
    public class LoginModel : PageModel

    {
        private readonly IConfiguration _configuration;
        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan LockoutDuration =
            TimeSpan.FromMinutes(5);
        private static readonly object LoginLock = new object();

        private static int failedAttempts = 0;

        private static DateTime? lockoutEndTime = null;

        public LoginModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        [BindProperty]
        public string Email { get; set; } = "";

        [BindProperty]
        public string Password { get; set; } = "";

        public string ErrorMessage { get; set; } = "";

        public IActionResult OnGet()
        {
            var otpVerified =
                HttpContext.Session.GetString("AdminOtpVerified");

            var expiryText =
                HttpContext.Session.GetString("AdminOtpVerifiedExpiry");

            if (otpVerified != "true" ||
                !DateTime.TryParse(
                    expiryText,
                    null,
                    System.Globalization.DateTimeStyles.RoundtripKind,
                    out var expiry) ||
                DateTime.UtcNow >= expiry)
            {
                HttpContext.Session.Remove("AdminOtpVerified");
                HttpContext.Session.Remove("AdminOtpVerifiedExpiry");

                return RedirectToPage("/Admin/RequestOtp");
            }

            return Page();
        }

        public IActionResult OnPost()
        {
            var otpVerified =
    HttpContext.Session.GetString("AdminOtpVerified");

            var expiryText =
                HttpContext.Session.GetString("AdminOtpVerifiedExpiry");

            if (otpVerified != "true" ||
                !DateTime.TryParse(
                    expiryText,
                    null,
                    System.Globalization.DateTimeStyles.RoundtripKind,
                    out var expiry) ||
                DateTime.UtcNow >= expiry)
            {
                HttpContext.Session.Remove("AdminOtpVerified");
                HttpContext.Session.Remove("AdminOtpVerifiedExpiry");

                return RedirectToPage("/Admin/RequestOtp");
            }

            lock (LoginLock)
            {
                // Check whether admin login is locked
                if (lockoutEndTime.HasValue)
                {
                    if (DateTime.UtcNow < lockoutEndTime.Value)
                    {
                        ErrorMessage =
                            "Too many failed attempts. Please try again after 5 minutes.";
                        return Page();
                    }

                    // Lockout period has expired
                    lockoutEndTime = null;
                    failedAttempts = 0;
                }

                var adminEmail = _configuration["AdminSettings:Email"];
                var adminPassword = _configuration["AdminSettings:Password"];

                if (!string.IsNullOrEmpty(adminEmail) &&
                    !string.IsNullOrEmpty(adminPassword) &&
                    Email == adminEmail &&
                    Password == adminPassword)
                {
                    failedAttempts = 0;
                    // Clear OTP verification after successful login
                    HttpContext.Session.Remove("AdminOtpVerified");
                    HttpContext.Session.Remove("AdminOtpVerifiedExpiry");

                    HttpContext.Session.SetString("AdminLoggedIn", "true");

                    return RedirectToPage("/Admin/Dashboard");
                }

                // Count incorrect login attempts
                failedAttempts++;

                if (failedAttempts >= MaxFailedAttempts)
                {
                    lockoutEndTime = DateTime.UtcNow.Add(LockoutDuration);

                    ErrorMessage =
                        "Too many failed attempts. Admin login is locked for 5 minutes.";
                }
                else
                {
                    ErrorMessage =
                        $"Invalid admin email or password. {MaxFailedAttempts - failedAttempts} attempts remaining.";
                }

                return Page();
            }
        }
    }
}