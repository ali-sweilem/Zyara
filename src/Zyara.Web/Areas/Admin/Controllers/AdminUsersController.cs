using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Zyara.Domain.Enums;
using Zyara.Services.Features.Administration.Dtos;
using Zyara.Services.Features.Administration.Interfaces;
using Zyara.Services.Features.Identity.Interfaces;

namespace Zyara.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminUsersController : Controller
    {
        private readonly IAdminUserService _users;
        private readonly ICurrentUser _currentUser;

        public AdminUsersController(IAdminUserService users, ICurrentUser currentUser)
        {
            _users = users;
            _currentUser = currentUser;
        }

        // /Admin/AdminUsers/Index?role=Doctor&search=ahmed&page=1
        [HttpGet]
        public async Task<IActionResult> Index(UserRole? role, string? search = null, int page = 1, CancellationToken cancellationToken = default)
        {
            var currentAdminId = _currentUser.RequireUserId();
            var model = new AdminUsersIndexDto
            {
                Users = await _users.GetUsersAsync(role, search, page, cancellationToken: cancellationToken),
                Role = role,
                Search = search,
                CurrentAdminId = currentAdminId
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id, UserRole? role, string? search = null, int page = 1, CancellationToken cancellationToken = default)
        {
            var result = await _users.ToggleUserActiveAsync(id, _currentUser.RequireUserId(), cancellationToken);
            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "User status updated." : result.Error;
            return RedirectToAction(nameof(Index), new { role, search, page });
        }
    }
}
