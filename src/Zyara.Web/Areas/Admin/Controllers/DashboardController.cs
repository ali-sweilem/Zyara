using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Enums;
using Zyara.Services.Data;

namespace Zyara.Web.Areas.Admin.Controllers;

[Authorize(Roles = "Admin")]
[Area("Admin")]
public class DashboardController : Controller
{
    private readonly ZyaraDbContext _db;
    public DashboardController(ZyaraDbContext db) => _db = db;

    [Route("Admin/Dashboard")]
    public async Task<IActionResult> Index()
    {
        ViewBag.UserCount = await _db.Users.CountAsync();
        ViewBag.PatientCount = await _db.Patients.CountAsync();
        ViewBag.DoctorCount = await _db.Doctors.CountAsync();
        ViewBag.NurseCount = await _db.Nurses.CountAsync();
        ViewBag.PendingVerification = await _db.Providers.CountAsync(p => p.VerificationStatus == VerificationStatus.Pending);
        return View();
    }

    [Route("Admin/Verification")]
    public async Task<IActionResult> Verification()
    {
        var providers = await _db.Providers
            .Select(p => new { p.Id, p.FullName, p.Email, p.Phone, p.Role, p.VerificationStatus, p.RejectionReason })
            .ToListAsync();
        return View(providers);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Verify(int id, VerificationStatus status, string? rejectionReason)
    {
        var provider = await _db.Providers.FirstOrDefaultAsync(p => p.Id == id);
        if (provider == null) return NotFound();
        provider.VerificationStatus = status;
        if (status == VerificationStatus.Rejected) provider.RejectionReason = rejectionReason ?? "Rejected by admin";
        else provider.RejectionReason = null;
        provider.VerifiedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return RedirectToAction("Verification");
    }

    [Route("Admin/Users")]
    public async Task<IActionResult> Users(string? filter)
    {
        var q = _db.Users.AsQueryable();
        if (!string.IsNullOrEmpty(filter)) q = q.Where(u => u.FullName.Contains(filter) || u.Email.Contains(filter) || u.Phone.Contains(filter));
        return View(await q.ToListAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, bool isActive)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();
        // Prevent self-deactivation and last admin
        if (user.Role == UserRole.Admin) return BadRequest("Cannot deactivate admin.");
        user.IsActive = isActive;
        await _db.SaveChangesAsync();
        return RedirectToAction("Users");
    }
}
