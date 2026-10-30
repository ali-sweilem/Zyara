using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Services.Data;
using Zyara.Web.Models;
using BCrypt.Net;
using System;

namespace Zyara.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly ZyaraDbContext _db;
        public AccountController(ZyaraDbContext db) => _db = db;

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string emailOrPhone, string password, string returnUrl = null)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == emailOrPhone || u.Phone == emailOrPhone);
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Invalid email/phone or password.");
                return View();
            }
            if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "Your account is inactive. Contact support.");
                return View();
            }
            // Role from DB — never trust client
            if (user is Provider p)
            {
                if (p.VerificationStatus == VerificationStatus.Pending)
                    return RedirectToAction("UnderReview");
                if (p.VerificationStatus == VerificationStatus.Rejected)
                    return RedirectToAction("UnderReview", new { rejected = true });
                // Verified provider -> existing Provider area
                return RedirectToAction("Index", "Dashboard", new { area = "Provider" });
            }
            if (user is Patient)
                return RedirectToAction("Placeholder", "Patient");
            if (user is Doctor || user is Nurse)
                return RedirectToAction("Placeholder", "Provider"); // or direct
            if (user is Admin)
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            return RedirectToLocal(returnUrl);
        }

        [HttpGet]
        public IActionResult UnderReview(bool rejected = false)
        {
            ViewBag.Rejected = rejected;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Minimal: sign out via cookie if using Identity; for now redirect
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (model.Role == UserRole.Admin) return BadRequest("Admin cannot self-register.");
            if (!model.TermsAccepted) ModelState.AddModelError(string.Empty, "Accept terms.");
            if (model.Password != model.ConfirmPassword) ModelState.AddModelError(string.Empty, "Passwords do not match.");
            if (await _db.Users.AnyAsync(u => u.Email == model.Email || u.Phone == model.Phone))
                ModelState.AddModelError(string.Empty, "Email or phone already in use.");
            if (!ModelState.IsValid) return View(model);

            var hash = BCrypt.Net.BCrypt.HashPassword(model.Password);
            User user = model.Role switch
            {
                UserRole.Doctor => new Doctor { FullName = model.FullName, Email = model.Email, Phone = model.Phone, Gender = model.Gender, PasswordHash = hash, Role = UserRole.Doctor, Specialty = "", VerificationStatus = VerificationStatus.Pending, LicenseNumber = model.LicenseNumber, ExperienceYears = 0 },
                UserRole.Nurse => new Nurse { FullName = model.FullName, Email = model.Email, Phone = model.Phone, Gender = model.Gender, PasswordHash = hash, Role = UserRole.Nurse, Specialty = "", VerificationStatus = VerificationStatus.Pending, ExperienceYears = 0 },
                _ => new Patient { FullName = model.FullName, Email = model.Email, Phone = model.Phone, Gender = model.Gender, PasswordHash = hash, Role = UserRole.Patient, DateOfBirth = model.DateOfBirth }
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            if (user is Provider prov && prov.VerificationStatus == VerificationStatus.Pending)
                return RedirectToAction("UnderReview");
            TempData["SuccessMessage"] = "Account created. Please sign in.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string emailOrPhone)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == emailOrPhone || u.Phone == emailOrPhone);
            if (user != null) { await CreateResetCode(user); }
            // Always same message regardless of existence
            return View("ForgotPasswordSuccess");
        }

        private async Task CreateResetCode(User user)
        {
            // Invalidate previous
            var old = await _db.PasswordResetCodes.Where(c => c.UserId == user.Id && !c.IsUsed).ToListAsync();
            foreach (var o in old) o.IsUsed = true;
            var code = new Random().Next(1000, 10000).ToString();
            _db.PasswordResetCodes.Add(new PasswordResetCode { UserId = user.Id, CodeHash = BCrypt.Net.BCrypt.HashPassword(code), CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddMinutes(5), AttemptCount = 0, IsUsed = false });
            await _db.SaveChangesAsync();
        }

        [HttpGet]
        public IActionResult ResetPassword(string token = null) => View();

        private IActionResult RedirectToLocal(string returnUrl) => Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction("Index", "Home");
    }
}
