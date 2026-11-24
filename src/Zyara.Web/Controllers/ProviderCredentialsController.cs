using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zyara.Services.Features.Authorization;
using Zyara.Services.Features.Identity.Interfaces;
using Zyara.Services.Features.Providers.Dtos;
using Zyara.Services.Features.Providers.Interfaces;

namespace Zyara.Web.Controllers
{
    public class ProviderCredentialsController : Controller
    {
        private readonly IProviderCredentialService _credentials;
        private readonly ICurrentUser _currentUser;
        private readonly IAuthorizationService _auth;

        public ProviderCredentialsController(
            IProviderCredentialService credentials,
            ICurrentUser currentUser,
            IAuthorizationService auth)
        {
            _credentials = credentials;
            _currentUser = currentUser;
            _auth = auth;
        }

        [Authorize(Policy = PolicyNames.ProviderOnly)]
        public async Task<IActionResult> Index()
        {
            var providerId = _currentUser.RequireUserId();
            var list = await _credentials.GetMyCredentialsAsync(providerId);
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = PolicyNames.ProviderOnly)]
        public async Task<IActionResult> Upload(UploadCredentialDto dto)
        {
            var providerId = _currentUser.RequireUserId();

            if (!ModelState.IsValid)
            {
                TempData["Error"] = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .First().ErrorMessage;
                return RedirectToAction(nameof(Index));
            }

            var result = await _credentials.UploadAsync(providerId, dto);

            if (result.Success) TempData["Success"] = "Document uploaded.";
            else TempData["Error"] = result.Error;

            return RedirectToAction(nameof(Index));
        }

        [Authorize]
        public async Task<IActionResult> Download(int id)
        {
            var credential = await _credentials.GetCredentialAsync(id);
            if (credential is null)
            {
                return NotFound();
            }

            var authorization = await _auth.AuthorizeAsync(User, credential, Operations.Download);
            if (!authorization.Succeeded)
            {
                return NotFound();
            }

            var file = await _credentials.GetFileAsync(id);

            if (file == null) return NotFound();

            return PhysicalFile(file.FullPath, file.ContentType, file.FileName);
        }
    }
}
