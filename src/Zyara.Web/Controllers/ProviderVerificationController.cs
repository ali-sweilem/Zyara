using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zyara.Services.Features.Authorization;
using Zyara.Services.Features.Identity.Interfaces;
using Zyara.Services.Features.Providers;
using Zyara.Services.Features.Providers.Interfaces;

namespace Zyara.Web.Controllers
{
    [Authorize(Policy = PolicyNames.ProviderOnly)]
    public class ProviderVerificationController : Controller
    {
        private readonly IProviderVerficationService _verification;
        private readonly ICurrentUser _currentUser;

        public ProviderVerificationController(
            IProviderVerficationService verification,
            ICurrentUser currentUser)
        {
            _verification = verification;
            _currentUser = currentUser;
        }

        public async Task<IActionResult> Status()
        {
            var providerId = _currentUser.RequireUserId();
            var dto = await _verification.GetStatusAsync(providerId);
            if (dto == null) return NotFound();

            return View(dto);
        }
    }
}
