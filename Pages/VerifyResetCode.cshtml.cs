using SMART_MALL_PARKING_SYSTEM.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SMART_MALL_PARKING_SYSTEM.Pages
{
    public class VerifyResetCodeModel : PageModel
    {
        private readonly EmailService _emailService;

        public VerifyResetCodeModel(EmailService emailService)
        {
            _emailService = emailService;
        }
        [BindProperty]
        public string VerificationCode { get; set; } = string.Empty;

        public IActionResult OnGet()
        {
            var resetEmail =
                HttpContext.Session.GetString("ResetPasswordEmail");

            if (string.IsNullOrEmpty(resetEmail))
            {
                return RedirectToPage("/ForgotPassword");
            }

            return Page();
        }

        public IActionResult OnPost()
        {
            var savedCode =
                HttpContext.Session.GetString("ResetPasswordCode");

            var expiryText =
                HttpContext.Session.GetString("ResetPasswordCodeExpiry");

            var resetEmail =
                HttpContext.Session.GetString("ResetPasswordEmail");

            if (string.IsNullOrEmpty(savedCode) ||
                string.IsNullOrEmpty(resetEmail))
            {
                return RedirectToPage("/ForgotPassword");
            }

            if (string.IsNullOrEmpty(expiryText) ||
                !DateTime.TryParse(expiryText, out var expiryTime) ||
                DateTime.Now > expiryTime)
            {
                ModelState.AddModelError(
                    "",
                    "Verification code has expired.");

                return Page();
            }

            if (VerificationCode != savedCode)
            {
                ModelState.AddModelError(
                    "",
                    "Invalid verification code.");

                return Page();
            }

            HttpContext.Session.SetString(
                "ResetPasswordVerified",
                "true");

            return RedirectToPage(
                "/ResetPassword",
                new { email = resetEmail });
        }
        public async Task<IActionResult> OnPostResendAsync()
        {
            ModelState.Clear();

            var resetEmail =
                HttpContext.Session.GetString("ResetPasswordEmail");

            if (string.IsNullOrEmpty(resetEmail))
            {
                return RedirectToPage("/ForgotPassword");
            }

            var newCode =
                Random.Shared.Next(100000, 1000000).ToString();

            HttpContext.Session.SetString(
                "ResetPasswordCode",
                newCode);

            HttpContext.Session.SetString(
                "ResetPasswordCodeExpiry",
                DateTime.Now.AddMinutes(2).ToString("O"));

            await _emailService.SendVerificationCodeAsync(
                resetEmail,
                newCode);

            TempData["SuccessMessage"] =
                "A new verification code has been sent to your email.";

            return RedirectToPage();
        }
    }
}