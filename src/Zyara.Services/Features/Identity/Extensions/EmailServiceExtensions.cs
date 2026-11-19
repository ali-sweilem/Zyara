using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Zyara.Services.Features.Identity.Interfaces;

namespace Zyara.Services.Features.Identity;

/// <summary>Stub by Ali (T1), completed by Abdelrhman (T7).</summary>
public static class EmailServiceExtensions
{
    public static IServiceCollection AddZyaraEmail(
        this IServiceCollection services, IConfiguration config, IHostEnvironment env)
    {
        services.Configure<EmailOptions>(config.GetSection("Email"));

        if (env.IsDevelopment())
        {
            services.AddScoped<IAppEmailSender, ConsoleEmailSender>();   // prints the link in the console
        }
        else
        {
            services.AddScoped<IAppEmailSender, SmtpEmailSender>();      // Email:* + user-secrets
        }

        services.AddScoped<IEmailConfirmationService, EmailConfirmationService>();
        return services;
    }
}
