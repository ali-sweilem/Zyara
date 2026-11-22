using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Zyara.Domain.Enums;
using Zyara.Domain.Rules;
using Zyara.Repositories.Data;
using Zyara.Services.Features.Bookings;
using Zyara.Services.Features.Providers;

namespace Zyara.Services.Jobs;

public class BookingExpiryWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _config;
    private readonly ILogger<BookingExpiryWorker> _logger;

    public BookingExpiryWorker(
        IServiceProvider serviceProvider,
        IConfiguration config,
        ILogger<BookingExpiryWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enabled = _config.GetValue<bool>("Booking:ExpiryJobEnabled", BookingRules.DefaultExpiryJobEnabled);
        if (!enabled)
        {
            _logger.LogInformation("BookingExpiryWorker is disabled via configuration.");
            return;
        }

        var intervalSeconds = _config.GetValue<int>("Booking:ExpiryJobIntervalSeconds", BookingRules.DefaultExpiryJobIntervalSeconds);
        if (intervalSeconds < 10) intervalSeconds = 10;

        _logger.LogInformation("BookingExpiryWorker started with interval {Interval} seconds.", intervalSeconds);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(intervalSeconds));

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessExpirationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in BookingExpiryWorker tick.");
            }
        }
    }

    private async Task ProcessExpirationsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZyaraDbContext>();
        var transitions = scope.ServiceProvider.GetRequiredService<IBookingTransitions>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        var egyptNow = EgyptTime.Now(timeProvider);
        var utcNow = DateTime.UtcNow;

        var paymentExpiryMinutes = _config.GetValue<int>("Booking:PendingPaymentExpiryMinutes", BookingRules.DefaultPendingPaymentExpiryMinutes);
        var approvalExpiryHours = _config.GetValue<int>("Booking:PendingApprovalExpiryHours", BookingRules.DefaultPendingApprovalExpiryHours);

        var paymentCutoff = utcNow.AddMinutes(-paymentExpiryMinutes);
        var approvalCutoff = utcNow.AddHours(-approvalExpiryHours);

        // 1. Expire stale PendingPayment bookings (exceeded payment timeout)
        var stalePaymentBookings = await db.Bookings
            .Include(b => b.TimeSlot)
            .Include(b => b.Payment)
            .Where(b => b.Status == BookingStatus.PendingPayment && b.CreatedAt <= paymentCutoff)
            .ToListAsync(stoppingToken);

        foreach (var booking in stalePaymentBookings)
        {
            try
            {
                _logger.LogInformation("Expiring stale PendingPayment booking {BookingId}.", booking.Id);
                await transitions.ExpireAsync(booking, utcNow, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to expire PendingPayment booking {BookingId}.", booking.Id);
            }
        }

        // 2. Expire stale PendingProviderApproval bookings (exceeded hours OR slot window start has passed in Egypt time)
        var staleApprovalBookings = await db.Bookings
            .Include(b => b.TimeSlot)
            .Include(b => b.Payment)
            .Where(b => b.Status == BookingStatus.PendingProviderApproval &&
                        (b.CreatedAt <= approvalCutoff ||
                         (b.TimeSlot != null &&
                          (b.TimeSlot.Date < egyptNow.Date ||
                           (b.TimeSlot.Date == egyptNow.Date && b.TimeSlot.StartTime <= egyptNow.TimeOfDay)))))
            .ToListAsync(stoppingToken);

        foreach (var booking in staleApprovalBookings)
        {
            try
            {
                _logger.LogInformation("Expiring stale PendingProviderApproval booking {BookingId}.", booking.Id);
                await transitions.ExpireAsync(booking, utcNow, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to expire PendingProviderApproval booking {BookingId}.", booking.Id);
            }
        }
    }
}

