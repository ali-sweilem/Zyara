using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Zyara.Services.Features.Administration.Dtos;
using Zyara.Services.Features.Administration.Interfaces;

namespace Zyara.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminFeesController : Controller
    {
        private readonly IAdminFeeService _fees;

        public AdminFeesController(IAdminFeeService fees)
        {
            _fees = fees;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            return View(await _fees.GetSettingsAsync(cancellationToken));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(PlatformSettingsDto dto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View(nameof(Index), dto);
            }

            var result = await _fees.UpdateSettingsAsync(dto, cancellationToken);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Could not save the settings.");
                return View(nameof(Index), dto);
            }

            TempData["Success"] = "Fees updated.";
            return RedirectToAction(nameof(Index));
        }
    }
}
