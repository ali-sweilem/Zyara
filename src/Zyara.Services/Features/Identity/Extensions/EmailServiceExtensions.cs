using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Zyara.Services.Features.Identity.Interfaces;

namespace Zyara.Services.Features.Identity;

/// <summary>STUB (T1 - Ali). Abdelrhman completes it in T7 (SmtpEmailSender + EmailConfirmationService).</summary>
public static class EmailServiceExtensions
{
    public static IServiceCollection AddZyaraEmail(
        this IServiceCollection services, IConfiguration config, IHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            services.AddScoped<IAppEmailSender, ConsoleEmailSender>();
        }
        // T7 (Abdelrhman): else -> services.AddScoped<IAppEmailSender, SmtpEmailSender>() (settings from "Email:*" + user-secrets)
        return services;
    }
}
