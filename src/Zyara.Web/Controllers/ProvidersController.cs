using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Threading;
using System.Threading.Tasks;
using Zyara.Domain.Enums;
using Zyara.Services.Features.Providers.Dtos;
using Zyara.Services.Features.Providers.Interfaces;
using Zyara.Web.Models;

namespace Zyara.Web.Controllers;


public sealed class ProvidersController : Controller
{
    private readonly IProviderDetailsService _providerDetailsService;
    private readonly IWebHostEnvironment _env;
  

    public ProvidersController(IProviderDetailsService providerDetailsService, IWebHostEnvironment env)
    {
        _providerDetailsService = providerDetailsService;
        _env = env;

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
        return View(new ProviderDetailsViewModel
        {
            Provider = provider,
            Reviews = reviews
        });
    }
    //==============================================profile==============================================
    //  /Providers/showprofile/5
    public async Task<IActionResult> showprofile(int id)
    {
        var model = await _providerDetailsService.showprofile(id);
        if (model == null)
        {
            return NotFound();
        }
        return View(model);
    }

    [HttpGet]

    public async Task<IActionResult> EditProfile(int id)
    {
        var model = await _providerDetailsService.GetForEditAsync(id);
        if (model == null)
        {
            return NotFound();
        }
        return View(model);
    }
  
    [HttpPost]
    public async Task<IActionResult> EditProfile(EditProfileProviderDto model)
    {
    
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
        return RedirectToAction(nameof(showprofile), new { id = model.Id });
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

