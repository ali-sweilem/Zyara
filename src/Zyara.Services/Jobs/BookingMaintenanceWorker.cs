using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Zyara.Services.Jobs;

/// <summary>
/// Background job: wakes up every minute and runs <see cref="IBookingMaintenanceService"/>.
/// A BackgroundService is a singleton, but the DbContext is scoped, so each run creates its own scope.
/// An error in one run is logged and the next run still happens.
/// </summary>
public sealed class BookingMaintenanceWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(15);   // let migrations / seeding finish first

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingMaintenanceWorker> _logger;

    public BookingMaintenanceWorker(IServiceScopeFactory scopeFactory, ILogger<BookingMaintenanceWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartupDelay, stoppingToken);

            using var timer = new PeriodicTimer(Interval);
            do
            {
                await RunOnceAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // application is shutting down
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IBookingMaintenanceService>();
            var result = await service.RunAsync(ct);

            if (result.Expired > 0 || result.NoShows > 0)
                _logger.LogInformation("Booking maintenance: {Expired} expired, {NoShows} no-show.", result.Expired, result.NoShows);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Booking maintenance run failed; it will retry on the next tick.");
        }
    }
}
