using Microsoft.Extensions.DependencyInjection;

namespace Zyara.Services.Features.Authorization;

/// <summary>STUB (T1 - Ali). Mostafa fills it in T6: register the resource-based AuthorizationHandlers here.</summary>
public static class ResourceAuthorizationExtensions
{
    public static IServiceCollection AddZyaraResourceAuthorization(this IServiceCollection services)
    {
        // T6 (Mostafa): services.AddScoped<IAuthorizationHandler, BookingAuthorizationHandler>(); ...
        return services;
    }
}
