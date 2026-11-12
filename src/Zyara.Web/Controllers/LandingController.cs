using Microsoft.AspNetCore.Mvc;
using Zyara.Web.Models;

namespace Zyara.Web.Controllers;

public class LandingController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View(new LandingIndexViewModel
        {
            Services = ServiceCatalog.Options
        });
    }
}
