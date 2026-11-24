using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
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

        // Register Google only when the keys are present, so startup does not fail before the secrets are set.
        // (No keys => no "Continue with Google" button anywhere: the views check for the "Google" scheme.)
        if (!string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(clientSecret))
        {
            services.AddAuthentication()
                .AddGoogle(options =>
                {
                    options.ClientId = clientId;
                    options.ClientSecret = clientSecret;

                    // Same cookie rules as the app cookie. Lax is enough for Google's top-level GET redirect and
                    // avoids "Correlation failed" when the site runs on plain http (localhost / LAN).
                    options.CorrelationCookie.SameSite = SameSiteMode.Lax;
                    options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

                    // User pressed "Cancel" on Google's page (access_denied) or the handshake failed:
                    // go back to the login page with a message instead of an unhandled exception page.
                    options.Events.OnRemoteFailure = ctx =>
                    {
                        ctx.Response.Redirect("/ExternalLogin/Callback?remoteError=failed");
                        ctx.HandleResponse();
                        return Task.CompletedTask;
                    };
                });
        }

        return services;
    }
}
