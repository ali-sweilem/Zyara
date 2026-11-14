using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Zyara.Services.Data;
using Zyara.Services.Features.AdminDashboard;
using Zyara.Services.Features.AdminDashboard.Interfaces;
using Zyara.Services.Features.Administration;
using Zyara.Services.Features.Administration.Interfaces;
using Zyara.Services.Features.Providers;
using Zyara.Services.Features.Providers.Interfaces;

namespace Zyara.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddZyaraServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<ZyaraDbContext>(o => o.UseSqlServer(config.GetConnectionString("Default")));
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<IProviderSearchService, ProviderSearchService>();
        services.AddScoped<IProviderDetailsService, ProviderDetailsService>();
        services.AddScoped<IProviderDashboardService, ProviderDashboardService>();
        services.AddScoped<IProviderVerficationService, ProviderVerificationService>();
        services.AddScoped<IProviderCredentialService, ProviderCredentialService>();

        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddScoped<IAdminFeeService, AdminFeeService>();
        services.AddScoped<IAdminServiceCatalogService, AdminServiceCatalogService>();
        services.AddScoped<IAdminBookingService, AdminBookingService>();
        services.AddScoped<IAdminDashboardService, AdminDashboardService>();
        services.AddScoped<IAdminProviderReviewService, AdminProviderReviewService>();
        return services;
    }
}