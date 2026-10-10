using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SMART_MALL_PARKING_SYSTEM.Pages.Admin
{
    public class VerifyOtpModel : PageModel
    {
        public IActionResult OnGet()
        {
            var pendingEmail =
                HttpContext.Session.GetString("AdminOtpPendingEmail");

            var otpExpiry =
                HttpContext.Session.GetString("AdminOtpExpiry");

            if (string.IsNullOrEmpty(pendingEmail) ||
                string.IsNullOrEmpty(otpExpiry))
            {
                return RedirectToPage("/Index");
            }

            if (!DateTime.TryParse(
                    otpExpiry,
                    null,
                    System.Globalization.DateTimeStyles.RoundtripKind,
                    out var expiry) ||
                DateTime.UtcNow >= expiry)
            {
                HttpContext.Session.Remove("AdminOtpPendingEmail");
                HttpContext.Session.Remove("AdminOtpExpiry");
                HttpContext.Session.Remove("AdminOtpCode");

                return RedirectToPage("/Index");
            }

            return Page();
        }
        [BindProperty]
        public string OtpCode { get; set; } = "";

        public IActionResult OnPost()
        {
            var pendingEmail =
                HttpContext.Session.GetString("AdminOtpPendingEmail");

            var savedCode =
                HttpContext.Session.GetString("AdminOtpCode");

            var expiryText =
                HttpContext.Session.GetString("AdminOtpExpiry");

            if (string.IsNullOrEmpty(pendingEmail) ||
                string.IsNullOrEmpty(savedCode) ||
                !DateTime.TryParse(
                    expiryText,
                    null,
                    System.Globalization.DateTimeStyles.RoundtripKind,
                    out var expiry))
            {
                return RedirectToPage("/Admin/RequestOtp");
            }

            // Check whether the OTP has expired
            if (DateTime.UtcNow >= expiry)
            {
                ClearPendingOtp();

                ModelState.AddModelError(
                    "",
                    "Verification code expired. Please request a new code.");

                return Page();
            }

            // Limit incorrect OTP attempts
            int attempts =
                HttpContext.Session.GetInt32("AdminOtpAttempts") ?? 0;

            if (attempts >= 5)
            {
                ClearPendingOtp();
                return RedirectToPage("/Admin/RequestOtp");
            }

            if (OtpCode != savedCode)
            {
                attempts++;

                HttpContext.Session.SetInt32(
                    "AdminOtpAttempts", attempts);

                if (attempts >= 5)
                {
                    ClearPendingOtp();

                    ModelState.AddModelError(
                        "",
                        "Too many incorrect codes. Request a new code.");
                }
                else
                {
                    ModelState.AddModelError(
                        "",
                        $"Invalid code. {5 - attempts} attempts remaining.");
                }

                return Page();
            }

            // OTP verified successfully
            ClearPendingOtp();

            HttpContext.Session.SetString(
                "AdminOtpVerified", "true");

            HttpContext.Session.SetString(
                "AdminOtpVerifiedExpiry",
                DateTime.UtcNow.AddMinutes(5).ToString("O"));

            return RedirectToPage("/Admin/Login");
        }

        private void ClearPendingOtp()
        {
            HttpContext.Session.Remove("AdminOtpPendingEmail");
            HttpContext.Session.Remove("AdminOtpCode");
            HttpContext.Session.Remove("AdminOtpExpiry");
            HttpContext.Session.Remove("AdminOtpAttempts");
        }
    }
}