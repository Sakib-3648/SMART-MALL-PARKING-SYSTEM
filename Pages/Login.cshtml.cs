using Microsoft.AspNetCore.Identity;
using SMART_MALL_PARKING_SYSTEM.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMART_MALL_PARKING_SYSTEM.Data;

public class LoginModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher;

    public LoginModel(ApplicationDbContext context)
    {
        _context = context;
        _passwordHasher = new PasswordHasher<User>();
    }

    [BindProperty]
    public string Email { get; set; }

    [BindProperty]
    public string Password { get; set; }

    public string ErrorMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _context.Users
    .FirstOrDefaultAsync(u => u.Email == Email);

        if (user == null)
        {
            ErrorMessage = "Invalid email or password.";
            return Page();
        }

        var passwordValid = false;

        if (user.Password.StartsWith("AQAAAA"))
        {
            var result = _passwordHasher.VerifyHashedPassword(
                user,
                user.Password,
                Password);

            passwordValid =
                result != PasswordVerificationResult.Failed;
        }
        else
        {
            passwordValid = user.Password == Password;

            if (passwordValid)
            {
                user.Password =
                    _passwordHasher.HashPassword(user, Password);

                await _context.SaveChangesAsync();
            }
        }

        if (!passwordValid)
        {
            ErrorMessage = "Invalid email or password.";
            return Page();
        }
        HttpContext.Session.SetInt32("UserId", user.UserId);
        return RedirectToPage("/Parking/Availability");
    }
}