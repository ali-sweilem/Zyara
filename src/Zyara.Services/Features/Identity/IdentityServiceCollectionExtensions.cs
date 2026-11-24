using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Repositories.Data;
using Zyara.Services.Features.Authorization;
using Zyara.Services.Features.Identity.Interfaces;
using Zyara.Services.Features.Payments;

namespace Zyara.Services.Features.Identity;

public static class IdentityServiceCollectionExtensions
{
    public static IServiceCollection AddZyaraIdentity(
        this IServiceCollection services, IConfiguration config, IHostEnvironment env)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IRegistrationService, RegistrationService>();   // T3 (Abdelrhman)
        services.AddScoped<ILoginService, LoginService>();
        services.AddScoped<ITwoFactorService, TwoFactorService>();   // T10 (Youssef)

        services
            .AddIdentity<User, IdentityRole<int>>(options =>
            {
                // Password policy
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;

                // Lockout: 5 failed attempts -> 10 minutes
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);

                // UserName = Email, so don't reject valid emails because of the default allowed-characters list
                options.User.RequireUniqueEmail = true;
                options.User.AllowedUserNameCharacters = string.Empty;

                // T7 (Abdelrhman): set to true after merge + testing (tell Ali first)
                options.SignIn.RequireConfirmedEmail = true;
            })
            .AddEntityFrameworkStores<ZyaraDbContext>()
            .AddDefaultTokenProviders();

        // Confirm-email / reset-password / 2FA tokens live 30 minutes
        services.Configure<DataProtectionTokenProviderOptions>(o => o.TokenLifespan = TimeSpan.FromMinutes(30));

        // Cookie paths (agreed with Youssef, T4)
        services.ConfigureApplicationCookie(o =>
        {
            o.LoginPath = "/Account/Login";
            o.LogoutPath = "/Account/Logout";
            o.AccessDeniedPath = "/Account/AccessDenied";
            o.Cookie.Name = ".Zyara.Auth";
            o.Cookie.HttpOnly = true;
            o.Cookie.SameSite = SameSiteMode.Lax;
            o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        });

        // Role policies (role names == UserRole enum names == seeded Identity roles)
        services.AddAuthorizationBuilder()
            .AddPolicy(PolicyNames.AdminOnly, p => p.RequireRole(nameof(UserRole.Admin)))
            .AddPolicy(PolicyNames.ProviderOnly, p => p.RequireRole(nameof(UserRole.Doctor), nameof(UserRole.Nurse)))
            .AddPolicy(PolicyNames.PatientOnly, p => p.RequireRole(nameof(UserRole.Patient)));

        // The 5 stubs (owners fill them in their tasks)
        services.AddZyaraEmail(config, env);
        services.AddZyaraCookieRefresh();
        services.AddZyaraExternalLogins(config);
        services.AddZyaraCardPayments(config);
        services.AddZyaraResourceAuthorization();

        return services;
    }
}
