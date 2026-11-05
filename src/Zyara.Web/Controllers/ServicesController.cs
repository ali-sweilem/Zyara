using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Zyara.Services.Features.Services;
using Zyara.Web.Models;

namespace Zyara.Web.Controllers;

public sealed class ServicesController : Controller
{
    private readonly IServiceManagementService _serviceManagementService;

    public ServicesController(
        IServiceManagementService serviceManagementService)
    {
        _serviceManagementService = serviceManagementService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken = default)
    {
        var services = await _serviceManagementService
            .GetAllAsync(cancellationToken);

        return View(new ServiceIndexViewModel
        {
            Services = services
        });
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateServiceDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        await _serviceManagementService.CreateAsync(dto, cancellationToken);

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(
        int id,
        CancellationToken cancellationToken = default)
    {
        var service = await _serviceManagementService
            .GetByIdAsync(id, cancellationToken);

        if (service is null)
        {
            return NotFound();
        }

        return View(new UpdateServiceDto
        {
            Name = service.Name,
            Description = service.Description,
            Type = service.Type
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        UpdateServiceDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var updated = await _serviceManagementService
            .UpdateAsync(id, dto, cancellationToken);

        if (!updated.Succeeded)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var updated = await _serviceManagementService
            .SetActiveStatusAsync(id, isActive, cancellationToken);

        if (!updated.Succeeded)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }
}