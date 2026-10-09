using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SMART_MALL_PARKING_SYSTEM.Pages.Admin
{
    public class LogoutModel : PageModel
    {
        public IActionResult OnGet()
        {
            HttpContext.Session.Remove("AdminLoggedIn");

            return RedirectToPage("/Admin/Login");
        }
    }
}