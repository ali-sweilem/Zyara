using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Zyara.Services.Features.Identity;

/// <summary>STUB (T1 - Ali). Omar fills it in T11 (Google).</summary>
public static class ExternalLoginServiceExtensions
{
    public static IServiceCollection AddZyaraExternalLogins(this IServiceCollection services, IConfiguration config)
    {
        // T11 (Omar): services.AddAuthentication().AddGoogle(...) from Authentication:Google:ClientId/ClientSecret.
        // Tip: register Google only when ClientId is not empty, otherwise the app fails at startup.
        return services;
    }
}
