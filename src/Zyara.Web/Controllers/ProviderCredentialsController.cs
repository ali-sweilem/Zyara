using Microsoft.AspNetCore.Mvc;
using Zyara.Services.Features.Providers.Dtos;
using Zyara.Services.Features.Providers.Interfaces;

namespace Zyara.Web.Controllers
{
    public class ProviderCredentialsController : Controller
    {
        private readonly IProviderCredentialService _credentials;

        public ProviderCredentialsController(IProviderCredentialService credentials)
        {
            _credentials = credentials;
        }

        public async Task<IActionResult> Index(int providerId)
        {
            ViewBag.ProviderId = providerId;
            var list = await _credentials.GetMyCredentialsAsync(providerId);
            return View(list);
        }

        [HttpPost]
        public async Task<IActionResult> Upload(int providerId, UploadCredentialDto dto)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .First().ErrorMessage;
                return RedirectToAction(nameof(Index), new { providerId });
            }

            var result = await _credentials.UploadAsync(providerId, dto);

            if (result.Success) TempData["Success"] = "Document uploaded.";
            else TempData["Error"] = result.Error;

            return RedirectToAction(nameof(Index), new { providerId });
        }

        public async Task<IActionResult> Download(int id, int providerId)
        {
            var file = await _credentials.GetFileAsync(id, providerId, User.IsInRole("Admin"));

            if (file == null) return NotFound();
            if (!file.IsAllowed) return Forbid();

            return PhysicalFile(file.FullPath, file.ContentType, file.FileName);
        }
    }
}