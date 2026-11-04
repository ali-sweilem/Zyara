using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Zyara.Services.Features.Providers;
using Zyara.Web.Models;

namespace Zyara.Web.Controllers;

public sealed class ProvidersController : Controller
{
    private readonly IProviderDetailsService _providerDetailsService;

    public ProvidersController(IProviderDetailsService providerDetailsService)
    {
        _providerDetailsService = providerDetailsService;
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, int reviewsPage = 1, CancellationToken cancellationToken = default)
    {
        var provider = await _providerDetailsService.GetDetailsAsync(id, cancellationToken);
        if (provider is null)
        {
            return NotFound();
        }

        var reviews = await _providerDetailsService.GetReviewsAsync(id, reviewsPage, 5, cancellationToken);
        return View(new ProviderDetailsViewModel
        {
            Provider = provider,
            Reviews = reviews
        });
    }
}
