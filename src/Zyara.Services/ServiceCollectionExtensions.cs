using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Zyara.Repositories.Data;
using Zyara.Repositories.Data.Repositories;
using Zyara.Services.Features.Administration;
using Zyara.Services.Features.Administration.Interfaces;
using Zyara.Services.Features.Bookings;
using Zyara.Services.Features.Bookings.Interfaces;
using Zyara.Services.Features.Identity;
using Zyara.Services.Features.MedicalRecords;
using Zyara.Services.Features.MedicalRecords.Interfaces;
using Zyara.Services.Features.Notifications;
using Zyara.Services.Features.Notifications.Interfaces;
using Zyara.Services.Features.Patients;
using Zyara.Services.Features.Wallet;
using Zyara.Services.Features.Wallet.Interfaces;
using Zyara.Services.Features.Payments;
using Zyara.Services.Features.Payments.Interface;
using Zyara.Services.Features.Providers;
using Zyara.Services.Features.Providers.Interfaces;
using Zyara.Services.Features.Reviews;
using Zyara.Services.Features.Reviews.Interfaces;
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
        services.AddScoped<IProviderServiceCatalogService, ProviderServiceCatalogService>();
        services.AddScoped<IWeeklyScheduleService, WeeklyScheduleService>();
        services.AddHostedService<WeeklySlotGeneratorJob>();   // keeps the next 4 weeks of weekly slots ready

        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IMedicalRecordService, MedicalRecordService>();
        services.AddScoped<IWalletService, WalletService>();
        services.AddScoped<IPatientPortalService, PatientPortalService>();
        services.AddScoped<IPatientProfileService, PatientProfileService>();
        services.AddScoped<ICareRecipientService, CareRecipientService>();
        services.AddScoped<IReviewService, ReviewService>();

        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IProviderBookingService, ProviderBookingService>();   // provider booking lifecycle

        // Moved here from Program.cs (T1)
        services.AddScoped<IPaymentService, PaymentService>();

        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddScoped<IAdminFeeService, AdminFeeService>();
        services.AddScoped<IAdminServiceCatalogService, AdminServiceCatalogService>();
        services.AddScoped<IAdminBookingService, AdminBookingService>();
        services.AddScoped<IAdminProviderReviewService, AdminProviderReviewService>();


        services.AddScoped<IBookingMaintenanceService, BookingMaintenanceService>();
        services.AddHostedService<BookingMaintenanceWorker>();
        // Identity + the 5 stubs (T1)
        services.AddZyaraIdentity(config, env);
        return services;
    }
}
