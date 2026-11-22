using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Zyara.Domain.Enums;
using Zyara.Services.Features.Identity.Interfaces;

namespace Zyara.Web.Areas.Provider.Controllers;

[Area("Provider")]
[AllowAnonymous]
public abstract class ProviderAreaController : Controller
{
    protected readonly ICurrentUser _currentUser;
    protected readonly IWebHostEnvironment _env;

    protected ProviderAreaController(ICurrentUser currentUser, IWebHostEnvironment env)
    {
        _currentUser = currentUser;
        _env = env;
    }

    protected int? CurrentProviderId
    {
        get
        {
            if (_currentUser.IsAuthenticated && _currentUser.UserId.HasValue)
            {
                return _currentUser.UserId.Value;
            }

            if (_env.IsDevelopment())
            {
                return 1;
            }

            return null;
        }
    }

    protected IActionResult? RequireProvider(out int providerId)
    {
        var id = CurrentProviderId;
        if (!id.HasValue)
        {
            providerId = 0;
            return Challenge();
        }

        // If authenticated as a non-provider in production, forbid
        if (_currentUser.IsAuthenticated &&
            _currentUser.Role.HasValue &&
            _currentUser.Role != UserRole.Doctor &&
            _currentUser.Role != UserRole.Nurse &&
            !_env.IsDevelopment())
        {
            providerId = 0;
            return Forbid();
        }

        providerId = id.Value;
        return null;
    }
}

