using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Zyara.Services.Features.Identity.Business_Logic;
using Zyara.Services.Features.Identity.Interfaces;

namespace Zyara.Services.Features.Identity;

/// <summary>Filled by Omar (T11 - Google OAuth).</summary>
public static class ExternalLoginServiceExtensions
{
    public static IServiceCollection AddZyaraExternalLogins(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IPasswordRecoveryService, PasswordRecoveryService>();
        services.AddScoped<IExternalLoginService, ExternalLoginService>();

        var clientId = config["Authentication:Google:ClientId"];
        var clientSecret = config["Authentication:Google:ClientSecret"];

        // Register Google only when ClientId is present to avoid failing startup when secrets are not yet configured
        if (!string.IsNullOrWhiteSpace(clientId) && 
            !string.IsNullOrWhiteSpace(clientSecret) && 
            !clientId.Contains("YOUR_CLIENT_ID", StringComparison.OrdinalIgnoreCase))
        {
            services.AddAuthentication()
                .AddGoogle(options =>
                {
                    options.SignInScheme = Microsoft.AspNetCore.Identity.IdentityConstants.ExternalScheme;
                    options.ClientId = clientId;
                    options.ClientSecret = clientSecret;
                });
        }

        return services;
    }
}
