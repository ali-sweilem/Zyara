using Microsoft.AspNetCore.Mvc;
using Zyara.Domain.Enums;
using Zyara.Services.Features.Administration.Interfaces;
using Zyara.Services.Features.Identity.Interfaces;

namespace Zyara.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminProvidersController : Controller
    {
        private readonly IAdminProviderReviewService _service;
        private readonly ICurrentUser _currentUser;

        public AdminProvidersController(IAdminProviderReviewService service, ICurrentUser currentUser)
        {
            _service = service;
            _currentUser = currentUser;
        }

        [HttpGet]
        public async Task<IActionResult> Index(VerificationStatus? status = VerificationStatus.Pending, bool all = false, CancellationToken cancellationToken = default)
        {
            VerificationStatus? filter = all ? null : status;
            ViewData["SelectedStatus"] = filter;
            ViewData["ShowAll"] = all;
            return View(await _service.GetProvidersAsync(filter, cancellationToken));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Review(
            int id,
            bool approve,
            string? reason,
            VerificationStatus? returnStatus = VerificationStatus.Pending,
            bool all = false,
            CancellationToken cancellationToken = default)
        {
            var result = await _service.ReviewProviderAsync(id, approve, reason, _currentUser.RequireUserId(), cancellationToken);

            if (result.Succeeded)
                TempData["Success"] = approve ? "Provider verified." : "Provider rejected.";
            else
                TempData["Error"] = result.Error;

            return RedirectToAction(nameof(Index), new { status = returnStatus, all });
        }
    }
}
