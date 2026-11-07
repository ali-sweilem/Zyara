using Microsoft.AspNetCore.Mvc;
using Zyara.Services.Features.Providers;
using Zyara.Services.Features.Providers.Interfaces;

namespace Zyara.Web.Controllers
{
    public class ProviderVerificationController : Controller
    {
        private readonly IProviderVerficationService _verification;

        public ProviderVerificationController(IProviderVerficationService verification)
        {
            _verification = verification;
        }

        public async Task<IActionResult> Status(int providerId)
        {
            var dto = await _verification.GetStatusAsync(providerId);
            if (dto == null) return NotFound();

            ViewBag.ProviderId = providerId;
            return View(dto);
        }
    }
}