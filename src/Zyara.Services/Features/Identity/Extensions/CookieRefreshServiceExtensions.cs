using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Zyara.Services.Features.Identity.Interfaces;

namespace Zyara.Services.Features.Identity;

/// <summary>T9 (Adham): sliding expiration, SecurityStamp validation interval, AJAX 401, ISessionService.</summary>
public static class CookieRefreshServiceExtensions
{
    public static IServiceCollection AddZyaraCookieRefresh(this IServiceCollection services)
    {
        // Runs after AddIdentity, so it only adds the lifetime/events (paths + cookie name are set in AddZyaraIdentity).
        services.ConfigureApplicationCookie(o =>
        {
            o.ExpireTimeSpan = TimeSpan.FromMinutes(30);
            o.SlidingExpiration = true;   // renewed on activity; the keep-alive ping covers an idle-but-open page

            o.Events ??= new CookieAuthenticationEvents();

            // "Remember me" (persistent cookie) lives 14 days; a normal session keeps the 30-minute sliding window.
            o.Events.OnSigningIn = ctx =>
            {
                if (ctx.Properties.IsPersistent)
                {
                    var issued = ctx.Properties.IssuedUtc ?? DateTimeOffset.UtcNow;
                    ctx.Properties.ExpiresUtc = issued.AddDays(14);
                }
                return Task.CompletedTask;
            };

            // AJAX calls get 401 (the page JS sends the user to Login) instead of a 302 to the login HTML.
            var redirectToLogin = o.Events.OnRedirectToLogin;
            o.Events.OnRedirectToLogin = ctx =>
            {
                if (IsAjax(ctx.Request))
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }
                return redirectToLogin(ctx);
            };
        });

        // Check the SecurityStamp every minute (default 30): new Roles / Deactivate / Password change reach the cookie fast.
        services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromMinutes(1));

        services.AddScoped<ISessionService, SessionService>();
        return services;
    }

    private static bool IsAjax(HttpRequest request) =>
        string.Equals(request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
}
