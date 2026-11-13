using Microsoft.AspNetCore.Mvc;
using Zyara.Services.Features.Administration.Interfaces;

namespace Zyara.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminProvidersController : Controller
    {
        private readonly IAdminProviderReviewService _service;

        public AdminProvidersController(IAdminProviderReviewService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            return View(await _service.GetPendingProvidersAsync(cancellationToken));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Review(int id, bool approve, string? reason, CancellationToken cancellationToken)
        {
            var result = await _service.ReviewProviderAsync(id, approve, reason, cancellationToken);

            if (result.Succeeded)
                TempData["Success"] = approve ? "Provider approved." : "Provider rejected.";
            else
                TempData["Error"] = result.Error;

            return RedirectToAction(nameof(Index));
        }
    }
}