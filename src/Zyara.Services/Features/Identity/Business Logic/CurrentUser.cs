using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Zyara.Domain.Enums;
using Zyara.Services.Features.Identity.Interfaces;

namespace Zyara.Services.Features.Identity;

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public int? UserId
    {
        get
        {
            if (!IsAuthenticated) return null;
            var raw = Principal!.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(raw, out var id) ? id : (int?)null;
        }
    }

    public UserRole? Role
    {
        get
        {
            if (!IsAuthenticated) return null;
            foreach (var claim in Principal!.FindAll(ClaimTypes.Role))
            {
                if (Enum.TryParse<UserRole>(claim.Value, ignoreCase: true, out var role))
                {
                    return role;
                }
            }
            return null;
        }
    }

    public int RequireUserId() =>
        UserId ?? throw new UnauthorizedAccessException("Authentication is required.");
}
