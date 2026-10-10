using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SMART_MALL_PARKING_SYSTEM.Services;

namespace SMART_MALL_PARKING_SYSTEM.Pages.Admin
{
    public class RequestOtpModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;

        public RequestOtpModel(
            IConfiguration configuration,
            EmailService emailService)
        {
            _configuration = configuration;
            _emailService = emailService;
        }

        public void OnGet()
        {
        }
        public async Task<IActionResult> OnPostAsync()
        {
            var adminEmail = _configuration["AdminSettings:Email"];

            if (string.IsNullOrWhiteSpace(adminEmail))
            {
                ModelState.AddModelError(
                    "",
                    "Admin email is not configured.");

                return Page();
            }

            // Prevent repeated OTP requests within 60 seconds
            var lastRequest =
                HttpContext.Session.GetString("AdminOtpLastRequest");

            if (DateTime.TryParse(
                lastRequest,
                null,
                System.Globalization.DateTimeStyles.RoundtripKind,
                out var lastRequestTime))
            {
                if (DateTime.UtcNow < lastRequestTime.AddSeconds(60))
                {
                    ModelState.AddModelError(
                        "",
                        "Please wait 60 seconds before requesting another code.");

                    return Page();
                }
            }

            // Cancel any previous successful OTP verification
            HttpContext.Session.Remove("AdminOtpVerified");
            HttpContext.Session.Remove("AdminOtpVerifiedExpiry");
            // Generate a secure 6-digit OTP
            var otp = System.Security.Cryptography
                .RandomNumberGenerator.GetInt32(100000, 1000000)
                .ToString();

            // Send the code to the admin email
            await _emailService.SendAdminOtpAsync(adminEmail, otp);

            // Store the verification request in the session
            HttpContext.Session.SetString("AdminOtpPendingEmail", adminEmail);
            HttpContext.Session.SetString("AdminOtpCode", otp);
            HttpContext.Session.SetString(
                "AdminOtpExpiry",
                DateTime.UtcNow.AddMinutes(2).ToString("O"));

            HttpContext.Session.SetString(
                "AdminOtpLastRequest",
                DateTime.UtcNow.ToString("O"));

            HttpContext.Session.SetInt32("AdminOtpAttempts", 0);

            return RedirectToPage("/Admin/VerifyOtp");
        }
    }
}