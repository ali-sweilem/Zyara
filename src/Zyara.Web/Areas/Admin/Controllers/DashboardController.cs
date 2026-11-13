using Microsoft.AspNetCore.Mvc;
using Zyara.Services.Features.AdminDashboard.Interfaces;

namespace Zyara.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class DashboardController : Controller
{
    private readonly IAdminDashboardService _service;
    public DashboardController(IAdminDashboardService service) => _service = service;

    public async Task<IActionResult> Index()
    {
        var model = await _service.GetSummaryAsync();
        return View(model);
    }
}