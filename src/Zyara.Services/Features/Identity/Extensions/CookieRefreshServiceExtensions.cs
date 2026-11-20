using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Zyara.Services.Features.Identity;
using Zyara.Services.Features.Identity.Interfaces;

namespace Zyara.Services.Features.Identity;

/// <summary>
/// T9 (Adham): Configures sliding cookie expiration (30m), persistent cookie (14 days),
/// SecurityStamp validation interval (1m), AJAX 401 redirect behavior,
/// and registers <see cref="ISessionService"/> / <see cref="SessionService"/>.
/// Called by <see cref="IdentityServiceCollectionExtensions.AddZyaraIdentity"/>.
/// </summary>
public static class CookieRefreshServiceExtensions
{
    public static IServiceCollection AddZyaraCookieRefresh(this IServiceCollection services)
    {
        // ── 1. Application Cookie Configuration ───────────────────────
        services.ConfigureApplicationCookie(options =>
        {
            // Sliding expiration: cookie expires after 30 minutes of inactivity
            options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
            options.SlidingExpiration = true;

            // ── AJAX 401 Handling on Redirect to Login ────────────────
            // If the request contains X-Requested-With: XMLHttpRequest, respond with 401 Unauthorized
            // instead of a 302 redirect to the login page.
            var originalOnRedirectToLogin = options.Events.OnRedirectToLogin;
            options.Events.OnRedirectToLogin = context =>
            {
                if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }

                return originalOnRedirectToLogin != null
                    ? originalOnRedirectToLogin(context)
                    : Task.CompletedTask;
            };

            // ── Remember Me (Persistent Cookie) Lifetime: 14 Days ──────
            var originalOnSigningIn = options.Events.OnSigningIn;
            options.Events.OnSigningIn = context =>
            {
                if (context.Properties.IsPersistent)
                {
                    var fourteenDays = TimeSpan.FromDays(14);
                    context.Properties.ExpiresUtc = DateTimeOffset.UtcNow.Add(fourteenDays);
                    context.CookieOptions.MaxAge = fourteenDays;
                }

                return originalOnSigningIn != null
                    ? originalOnSigningIn(context)
                    : Task.CompletedTask;
            };
        });

        // ── 2. SecurityStamp Validation Interval (1 Minute) ────────────
        // Every 1 minute, the middleware compares the SecurityStamp stored
        // in the cookie with the database. When a role changes, account is deactivated,
        // or password is changed, the cookie is rejected within <= 1 minute.
        services.Configure<SecurityStampValidatorOptions>(options =>
        {
            options.ValidationInterval = TimeSpan.FromMinutes(1);
        });

        // ── 3. Session Service Registration ───────────────────────────
        services.AddScoped<ISessionService, SessionService>();

        // ── 4. Antiforgery Header Configuration ───────────────────────
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "RequestVerificationToken";
        });

        return services;
    }
}
