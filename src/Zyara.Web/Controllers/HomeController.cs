using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Zyara.Domain.Enums;
using Zyara.Services.Features.Providers;
using Zyara.Services.Features.Providers.Interfaces;
using Zyara.Web.ViewModels;
using Zyara.Web.Models;

namespace Zyara.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IProviderSearchService _providerSearchService;

        public HomeController(IProviderSearchService providerSearchService)
        {
            _providerSearchService = providerSearchService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            [FromQuery] string? service,
            [FromQuery] ProviderSearchCriteria criteria,
            CancellationToken cancellationToken)
        {
            var serviceRole = service?.Trim().ToLowerInvariant() switch
            {
                "doctors" => UserRole.Doctor,
                "nursing" => UserRole.Nurse,
                _ => (UserRole?)null
            };

            if (!serviceRole.HasValue)
            {
                return RedirectToAction("Index", "Landing");
            }

            criteria.ProviderType = serviceRole;

            // Input sanitation and validation
            if (criteria.PageNumber < 1)
            {
                criteria.PageNumber = 1;
            }

            criteria.PageSize = Math.Clamp(criteria.PageSize > 0 ? criteria.PageSize : 12, 1, 50);

            // Validate Gender enum value
            if (criteria.Gender.HasValue && !Enum.IsDefined(typeof(Gender), criteria.Gender.Value))
            {
                criteria.Gender = null;
            }

            // Validate ProviderType (only Doctor and Nurse are eligible provider roles)
            if (criteria.ProviderType.HasValue &&
                criteria.ProviderType != UserRole.Doctor &&
                criteria.ProviderType != UserRole.Nurse)
            {
                criteria.ProviderType = null;
            }

            // Price range sanity check
            if (criteria.MinPrice.HasValue && criteria.MinPrice.Value < 0)
            {
                criteria.MinPrice = 0;
            }

            if (criteria.MaxPrice.HasValue && criteria.MaxPrice.Value < 0)
            {
                criteria.MaxPrice = null;
            }

            if (criteria.MinPrice.HasValue && criteria.MaxPrice.HasValue && criteria.MinPrice > criteria.MaxPrice)
            {
                // Swap values if min is greater than max
                (criteria.MinPrice, criteria.MaxPrice) = (criteria.MaxPrice, criteria.MinPrice);
            }

            // Execute search
            var results = await _providerSearchService.SearchProvidersAsync(criteria, cancellationToken);

            // Edge case: requested page is greater than total pages
            if (results.TotalCount > 0 && criteria.PageNumber > results.TotalPages)
            {
                criteria.PageNumber = results.TotalPages;
                results = await _providerSearchService.SearchProvidersAsync(criteria, cancellationToken);
            }

            // Populate filter dropdowns from eligible providers in DB
            var specialties = await _providerSearchService.GetSpecialtiesAsync(serviceRole.Value, cancellationToken);
            var cities = await _providerSearchService.GetCitiesAsync(serviceRole.Value, cancellationToken);

            var model = new HomeIndexViewModel
            {
                Criteria = criteria,
                Results = results,
                Specialties = specialties,
                Cities = cities,
                Service = serviceRole == UserRole.Doctor ? "doctors" : "nursing"
            };

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }



    }
}
