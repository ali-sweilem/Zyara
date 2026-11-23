using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Zyara.Web.Controllers;

/// <summary>T9 (Adham): keep-alive ping. Hitting any authenticated endpoint renews the sliding cookie.</summary>
[Authorize]
public class SessionController : Controller
{
    // POST /Session/Refresh
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Refresh() => NoContent();
}
