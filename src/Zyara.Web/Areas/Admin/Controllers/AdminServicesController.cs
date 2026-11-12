using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Zyara.Services.Features.Administration.Interfaces;

namespace Zyara.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminServicesController : Controller
    {
        private readonly IAdminServiceCatalogService _services;

        public AdminServicesController(IAdminServiceCatalogService services)
        {
            _services = services;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            return View(await _services.GetServicesAsync(cancellationToken));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id, CancellationToken cancellationToken)
        {
            var found = await _services.ToggleServiceAsync(id, cancellationToken);
            TempData[found ? "Success" : "Error"] = found ? "Service status updated." : "Service not found.";
            return RedirectToAction(nameof(Index));
        }
    }
}
