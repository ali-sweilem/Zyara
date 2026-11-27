using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using Zyara.Domain.Enums;
using Zyara.Services.Features.Authorization;
using Zyara.Services.Features.Identity.Interfaces;
using Zyara.Services.Features.Providers.Dtos;
using Zyara.Services.Features.Providers.Interfaces;
using Zyara.Services.Features.Shared.Dtos;

namespace Zyara.Web.Controllers;


public sealed class ProvidersController : Controller
{
    private readonly IProviderDetailsService _providerDetailsService;
    private readonly IWebHostEnvironment _env;
    private readonly ICurrentUser _currentUser;

    public ProvidersController(
        IProviderDetailsService providerDetailsService,
        IWebHostEnvironment env,
        ICurrentUser currentUser)
    {
        _providerDetailsService = providerDetailsService;
        _env = env;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, int reviewsPage = 1, CancellationToken cancellationToken = default)
    {
        var provider = await _providerDetailsService.GetDetailsAsync(id, cancellationToken);
        if (provider is null)
        {
            return NotFound();
        }

        var reviews = await _providerDetailsService.GetReviewsAsync(id, reviewsPage, 5, cancellationToken);
        return View(new ProviderDetailsPageDto
        {
            Provider = provider,
            Reviews = reviews
        });
    }
    //==============================================profile==============================================
    [Authorize(Policy = PolicyNames.ProviderOnly)]
    public async Task<IActionResult> showprofile()
    {
        var providerId = _currentUser.RequireUserId();
        var model = await _providerDetailsService.showprofile(providerId);
        if (model == null)
        {
            return NotFound();
        }
        return View(model);
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.ProviderOnly)]
    public async Task<IActionResult> Settings()
    {
        var providerId = _currentUser.RequireUserId();
        var model = await _providerDetailsService.showprofile(providerId);
        if (model == null)
        {
            return NotFound();
        }
        return View(model);
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.ProviderOnly)]
    public async Task<IActionResult> EditProfile()
    {
        var providerId = _currentUser.RequireUserId();
        var model = await _providerDetailsService.GetForEditAsync(providerId);
        if (model == null)
        {
            return NotFound();
        }
        return View(model);
    }
  
    [HttpPost]
    [Authorize(Policy = PolicyNames.ProviderOnly)]
    public async Task<IActionResult> EditProfile(EditProfileProviderDto model)
    {
        model.Id = _currentUser.RequireUserId();

        if (!ModelState.IsValid)
        {
            return View(model);
        }
        if (model.PhotoFile is { Length: > 0 })
        {
            var ext = GetImageExtension(model.PhotoFile);
            if (ext is null)
            {
                ModelState.AddModelError(nameof(model.PhotoFile), "Please select a valid image file.");
                return View(model);
            }

            var folder = Path.Combine(_env.WebRootPath, "upload", "provider");
            Directory.CreateDirectory(folder);

            var filename = $"{Guid.NewGuid()}{ext}";
            using (var stream = new FileStream(Path.Combine(folder, filename), FileMode.Create))
            {
                await model.PhotoFile.CopyToAsync(stream);
            }

            model.PhotoUrl = $"/upload/provider/{filename}";
        }
        var success = await _providerDetailsService.UpdateProfileAsync(model);
        if (!success)
        {
            return NotFound();
        }
        TempData["Success"] = "Profile updated successfully.";
        return RedirectToAction(nameof(showprofile));
    }

    private static string? GetImageExtension(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp")
        {
            return extension;
        }

        return file.ContentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/webp" => ".webp",
            _ => null
        };
    }

}



