using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Zyara.Services.Features.Authorization.Handlers;

namespace Zyara.Services.Features.Authorization;

public static class ResourceAuthorizationExtensions
{
    public static IServiceCollection AddZyaraResourceAuthorization(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationHandler, BookingAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, CredentialAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, ServiceNoteAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, TimeSlotAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, ProviderProfileAuthorizationHandler>();

        return services;
    }
}
