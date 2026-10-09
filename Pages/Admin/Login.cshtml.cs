using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SMART_MALL_PARKING_SYSTEM.Pages.Admin
{
    public class LoginModel : PageModel
    {
        [BindProperty]
        public string Email { get; set; } = "";

        [BindProperty]
        public string Password { get; set; } = "";

        public string ErrorMessage { get; set; } = "";

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            if (Email == "admin@smartparking.com" &&
                Password == "Admin123")
            {
                HttpContext.Session.SetString("AdminLoggedIn", "true");

                return RedirectToPage("/Admin/Dashboard");
            }

            ErrorMessage = "Invalid admin email or password.";
            return Page();
        }
    }
}