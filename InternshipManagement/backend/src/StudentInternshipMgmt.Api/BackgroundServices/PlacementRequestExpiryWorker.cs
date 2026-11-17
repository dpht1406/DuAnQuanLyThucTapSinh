using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StudentInternshipMgmt.Application.Features.PlacementRequests;

namespace StudentInternshipMgmt.Api.BackgroundServices;

public sealed class PlacementRequestExpiryWorker : BackgroundService
{
    private const int DefaultIntervalMinutes = 60;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PlacementRequestExpiryWorker> _logger;

    public PlacementRequestExpiryWorker(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<PlacementRequestExpiryWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!(_configuration.GetValue<bool?>("PlacementRequestExpiry:Enabled") ?? true))
        {
            _logger.LogInformation("Placement request expiry worker is disabled");
            return;
        }

        var intervalMinutes = _configuration.GetValue<int?>("PlacementRequestExpiry:IntervalMinutes")
            ?? DefaultIntervalMinutes;
        if (intervalMinutes <= 0)
        {
            _logger.LogWarning(
                "Invalid PlacementRequestExpiry:IntervalMinutes value {IntervalMinutes}; using default {DefaultIntervalMinutes}",
                intervalMinutes,
                DefaultIntervalMinutes);
            intervalMinutes = DefaultIntervalMinutes;
        }

        var interval = TimeSpan.FromMinutes(intervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var expiryService = scope.ServiceProvider.GetRequiredService<IPlacementRequestExpiryService>();
                await expiryService.ExpireOverdueAsync(DateTime.UtcNow, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to expire overdue placement requests");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }
}
