using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Zyara.Domain.Enums;
using Zyara.Services.Features.Administration.Interfaces;
using Zyara.Web.ViewModels;

namespace Zyara.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminUsersController : Controller
    {
        private readonly IAdminUserService _users;

        public AdminUsersController(IAdminUserService users)
        {
            _users = users;
        }
        // / Admin/AdminUsers/Index?role=Doctor&page=1
        [HttpGet]
        public async Task<IActionResult> Index(UserRole? role, int page = 1, CancellationToken cancellationToken = default)
        {
            var model = new AdminUsersIndexViewModel
            {
                Users = await _users.GetUsersAsync(role, page, cancellationToken: cancellationToken),
                Role = role
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id, UserRole? role, int page = 1, CancellationToken cancellationToken = default)
        {
            var found = await _users.ToggleUserActiveAsync(id, cancellationToken);
            TempData[found ? "Success" : "Error"] = found ? "User status updated." : "User not found.";
            return RedirectToAction(nameof(Index), new { role, page });
        }
    }
}
