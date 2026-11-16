using Microsoft.AspNetCore.Mvc;
using Zyara.Services.Features.Shared.Dtos;

namespace Zyara.Web.Controllers;

public class LandingController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View(new LandingIndexDto
        {
            Services = ServiceCatalog.Options
        });
    }
}


