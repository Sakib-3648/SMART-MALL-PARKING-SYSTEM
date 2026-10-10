using Microsoft.AspNetCore.Identity;
using SMART_MALL_PARKING_SYSTEM.Services;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SMART_MALL_PARKING_SYSTEM.Data;
using SMART_MALL_PARKING_SYSTEM.Models;

public class RegisterModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly EmailService _emailService;
    private readonly PasswordHasher<User> _passwordHasher;

    public RegisterModel(
    ApplicationDbContext context,
    EmailService emailService)
    {
        _context = context;
        _emailService = emailService;
        _passwordHasher = new PasswordHasher<User>();
    }

    [BindProperty]
    public string FullName { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Password is required.")]
    [RegularExpression(
     @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$",
     ErrorMessage = "Password must have at least 8 characters, including uppercase, lowercase, number, and special character.")]
    public string Password { get; set; }

    [BindProperty]
    public string VehicleNumber { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }
        var emailExists = await _context.Users
    .AnyAsync(u => u.Email == Email);

        if (emailExists)
        {
            ModelState.AddModelError(
                "Email",
                "This email address is already registered.");

            return Page();
        }
        var verificationCode = Random.Shared.Next(100000, 1000000).ToString();
        HttpContext.Session.SetString("PendingFullName", FullName);
        HttpContext.Session.SetString("PendingEmail", Email);
        var temporaryUser = new User();

        var hashedPassword =
            _passwordHasher.HashPassword(temporaryUser, Password);

        HttpContext.Session.SetString(
            "PendingPassword",
            hashedPassword);
        HttpContext.Session.SetString("PendingVehicleNumber", VehicleNumber);
        HttpContext.Session.SetString("VerificationCode", verificationCode);
        HttpContext.Session.SetString(
    "VerificationCodeExpiry",
    DateTime.Now.AddMinutes(2).ToString("O"));
        await _emailService.SendVerificationCodeAsync(
    Email,
    verificationCode);

        return RedirectToPage("/VerifyEmail");
    }
}