using Zyara.Domain.Enums;

namespace Zyara.Web.Infrastructure;

/// <summary>
/// One place that decides where each role lands after signing in.
/// Used by AccountController (T4) and TwoFactorController (T10).
/// </summary>
public static class LoginRedirects
{
    public static string HomeFor(UserRole role) => role switch
    {
        UserRole.Admin => "/Admin/Dashboard",
        UserRole.Doctor or UserRole.Nurse => "/Provider/Schedule",
        _ => "/Home"
    };
}
