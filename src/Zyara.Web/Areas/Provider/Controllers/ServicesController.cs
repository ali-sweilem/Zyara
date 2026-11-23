using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zyara.Services.Features.Identity.Interfaces;
using Zyara.Services.Features.Providers.Dtos;
using Zyara.Services.Features.Providers.Interfaces;

namespace Zyara.Web.Areas.Provider.Controllers
{
    /// <summary>The services I offer to patients and their prices. The provider id always comes from ICurrentUser.</summary>
    [Area("Provider")]
    [Authorize(Roles = "Doctor,Nurse")]
    public sealed class ServicesController : Controller
    {
        private readonly IProviderServiceCatalogService _services;
        private readonly ICurrentUser _currentUser;

        public ServicesController(IProviderServiceCatalogService services, ICurrentUser currentUser)
        {
            _services = services;
            _currentUser = currentUser;
        }

        // GET /Provider/Services
        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var model = await _services.GetPageAsync(_currentUser.RequireUserId(), cancellationToken);
            return View(model);
        }

        // POST /Provider/Services/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(AddOfferedServiceDto dto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return Invalid();

            var result = await _services.AddAsync(_currentUser.RequireUserId(), dto, cancellationToken);
            return Done(result.Succeeded, "Service added.", result.Error);
        }

        // POST /Provider/Services/UpdateFee
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateFee(UpdateOfferedServiceDto dto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return Invalid();

            var result = await _services.UpdateFeeAsync(_currentUser.RequireUserId(), dto, cancellationToken);
            return Done(result.Succeeded, "Price updated.", result.Error);
        }

        // POST /Provider/Services/Remove
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id, CancellationToken cancellationToken)
        {
            var result = await _services.RemoveAsync(_currentUser.RequireUserId(), id, cancellationToken);
            return Done(result.Succeeded, "Service removed.", result.Error);
        }

        private IActionResult Invalid()
        {
            TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage
                                ?? "Please check the values you entered.";
            return RedirectToAction(nameof(Index));
        }

        private IActionResult Done(bool ok, string success, string? error)
        {
            if (ok) TempData["Message"] = success;
            else TempData["Error"] = error;
            return RedirectToAction(nameof(Index));
        }
    }
}
