using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Zyara.Services.Features.Providers.Interfaces;

namespace Zyara.Services.Jobs;

/// <summary>
/// Keeps every provider's weekly working times turned into real TimeSlots for the next 4 weeks.
/// Runs shortly after start-up and then every 6 hours, so slots keep appearing even if the provider never opens the page.
/// </summary>
public sealed class WeeklySlotGeneratorJob : BackgroundService
{
    private static readonly TimeSpan StartDelay = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<WeeklySlotGeneratorJob> _logger;

    public WeeklySlotGeneratorJob(IServiceScopeFactory scopes, ILogger<WeeklySlotGeneratorJob> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartDelay, stoppingToken);   // let migrations / seeding finish first

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    var weekly = scope.ServiceProvider.GetRequiredService<IWeeklyScheduleService>();
                    var created = await weekly.GenerateAsync(null, stoppingToken);

                    if (created > 0)
                    {
                        _logger.LogInformation("Weekly schedule: created {Count} time slots.", created);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // e.g. the migration for WeeklySlotTemplates was not applied yet: log and try again later.
                    _logger.LogError(ex, "Weekly slot generation failed.");
                }

                await Task.Delay(Interval, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // application is shutting down
        }
    }
}
