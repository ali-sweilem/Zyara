using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Zyara.Repositories.Data;
using Zyara.Repositories.Data.Repositories;
using Zyara.Services.Features.AdminDashboard;
using Zyara.Services.Features.AdminDashboard.Interfaces;
using Zyara.Services.Features.Administration;
using Zyara.Services.Features.Administration.Interfaces;
using Zyara.Services.Features.Bookings;
using Zyara.Services.Features.Bookings.Interfaces;
using Zyara.Services.Features.Identity;
using Zyara.Services.Features.Payments;
using Zyara.Services.Features.Payments.Interface;
using Zyara.Services.Features.Providers;
using Zyara.Services.Features.Providers.Interfaces;
using Zyara.Services.Jobs;

namespace Zyara.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddZyaraServices(
        this IServiceCollection services, IConfiguration config, IHostEnvironment env)
    {
        services.AddDbContext<ZyaraDbContext>(o => o.UseSqlServer(config.GetConnectionString("Default")));
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<IProviderSearchService, ProviderSearchService>();
        services.AddScoped<IProviderDetailsService, ProviderDetailsService>();
        services.AddScoped<IProviderVerficationService, ProviderVerificationService>();
        services.AddScoped<IProviderCredentialService, ProviderCredentialService>();

        services.AddScoped<IBookingTransitions, BookingTransitions>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IProviderDashboardService, ProviderDashboardService>();
        services.AddScoped<IProviderBookingService, ProviderBookingService>();
        services.AddScoped<IProviderScheduleService, ProviderScheduleService>();

        // Expiry background job
        services.AddHostedService<BookingExpiryWorker>();

        // Moved here from Program.cs (T1)
        services.AddScoped<IPaymentService, PaymentService>();

        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddScoped<IAdminFeeService, AdminFeeService>();
        services.AddScoped<IAdminServiceCatalogService, AdminServiceCatalogService>();
        services.AddScoped<IAdminBookingService, AdminBookingService>();
        services.AddScoped<IAdminDashboardService, AdminDashboardService>();
        services.AddScoped<IAdminProviderReviewService, AdminProviderReviewService>();

        // Identity + the 5 stubs (T1)
        services.AddZyaraIdentity(config, env);
        return services;
    }
}
