using Microsoft.Extensions.DependencyInjection;

namespace Zyara.Services.Features.Identity;

/// <summary>STUB (T1 - Ali). Adham fills it in T9: sliding expiration, SecurityStamp validation interval, ISessionService.</summary>
public static class CookieRefreshServiceExtensions
{
    public static IServiceCollection AddZyaraCookieRefresh(this IServiceCollection services)
    {
        // T9 (Adham): ConfigureApplicationCookie(...) + SecurityStampValidatorOptions + AddScoped<ISessionService, SessionService>()
        return services;
    }
}
