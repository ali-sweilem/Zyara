using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Services.Data;

namespace Zyara.Web.Controllers;

[Authorize]
public class ProviderCredentialController : Controller
{
    private readonly ZyaraDbContext _db;
    private readonly IFileUploadValidator _validator;

    public ProviderCredentialController(ZyaraDbContext db, IFileUploadValidator validator)
    {
        _db = db;
        _validator = validator;
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public IActionResult UploadAdmin(int providerId, IFormFile file)
    {
        // Admin can approve; upload handled by provider below
        return RedirectToAction("Index", "UnderReview", new { area = "" });
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file, string documentType)
    {
        var userEmail = User.Identity?.Name ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var user = await _db.Users.OfType<Provider>().FirstOrDefaultAsync(p => p.Email == userEmail);
        if (user == null) return Forbid();
        if (file == null || file.Length == 0) { ModelState.AddModelError("", "File required."); return RedirectToAction("UnderReview", "Account"); }
        if (!_validator.IsWithinSizeLimit(file.Length)) { ModelState.AddModelError("", "File exceeds 5 MB."); return RedirectToAction("UnderReview", "Account"); }
        if (!_validator.IsSafeFileName(file.FileName)) { ModelState.AddModelError("", "Invalid file name."); return RedirectToAction("UnderReview", "Account"); }
        if (!_validator.IsAllowedExtension(file.FileName) || !_validator.IsAllowedContentType(file.ContentType))
        {
            ModelState.AddModelError("", "Only PDF, JPG, PNG allowed.");
            return RedirectToAction("UnderReview", "Account");
        }

        // Magic bytes validation (simplified: check first bytes match extension)
        using var stream = file.OpenReadStream();
        var header = new byte[8];
        await stream.ReadAsync(header, 0, 8);
        bool validMagic = (file.FileName.EndsWith(".pdf") && header[0] == 0x25 && header[1] == 0x50) ||
                          (file.FileName.EndsWith(".jpg") || file.FileName.EndsWith(".jpeg")) && header[0] == 0xFF && header[1] == 0xD8 ||
                          file.FileName.EndsWith(".png") && header[0] == 0x89 && header[1] == 0x50;
        if (!validMagic) { ModelState.AddModelError("", "File content does not match extension."); return RedirectToAction("UnderReview", "Account"); }

        var fileName = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName).ToLowerInvariant();
        var folder = Path.Combine("C:", "ZyaraUploads"); // Outside wwwroot
        Directory.CreateDirectory(folder);
        var filePath = Path.Combine(folder, fileName);
        using (var fs = new FileStream(filePath, FileMode.Create))
        {
            file.CopyTo(fs);
        }

        _db.ProviderCredentials.Add(new ProviderCredential
        {
            DocumentType = documentType ?? Path.GetFileNameWithoutExtension(file.FileName),
            FileUrl = filePath,
            ProviderID = user.Id,
            UploadedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return RedirectToAction("UnderReview", "Account");
    }

    private int GetUserId()
    {
        // Read from DB or claim (simplified)
        return int.Parse(User.FindFirst("UserId")?.Value ?? "0");
    }
}
