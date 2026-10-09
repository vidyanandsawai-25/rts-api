using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Infrastructure.Services;

/// <summary>
/// Background service that automatically pushes daily RTS application statistics 
/// to the Government MahaIT RTS Central Dashboard every morning.
/// Document Reference: DashboardAPI_clientDoc_New.pdf
/// </summary>
public class MahaITDailyDashboardPushHostedService : BackgroundService
{
    private readonly ILogger<MahaITDailyDashboardPushHostedService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public MahaITDailyDashboardPushHostedService(
        ILogger<MahaITDailyDashboardPushHostedService> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MahaITDailyDashboardPushHostedService started.");

        // Check if daily push is enabled (default: true)
        bool isEnabled = _configuration.GetValue<bool?>("MahaITDashboard:DailyPushEnabled") ?? true;
        if (!isEnabled)
        {
            _logger.LogInformation("MahaITDailyDashboardPushHostedService is disabled via configuration.");
            return;
        }

        // Configurable morning push time (default: 6:00 AM)
        int targetHour = _configuration.GetValue<int?>("MahaITDashboard:MorningPushHour") ?? 6;
        int targetMinute = _configuration.GetValue<int?>("MahaITDashboard:MorningPushMinute") ?? 0;

        // Check if run on startup is requested (default: false)
        bool runOnStartup = _configuration.GetValue<bool?>("MahaITDashboard:RunOnStartup") ?? false;
        if (runOnStartup)
        {
            _logger.LogInformation("MahaITDailyDashboardPushHostedService: Executing initial push on startup...");
            await ExecutePushAsync(stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.Now;
                var nextRun = new DateTime(now.Year, now.Month, now.Day, targetHour, targetMinute, 0);

                if (now >= nextRun)
                {
                    nextRun = nextRun.AddDays(1);
                }

                var delay = nextRun - now;
                _logger.LogInformation("MahaITDailyDashboardPushHostedService: Next scheduled morning push is at {NextRun} (waiting {Delay:hh\\:mm\\:ss})", 
                    nextRun, delay);

                await Task.Delay(delay, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                    break;

                _logger.LogInformation("MahaITDailyDashboardPushHostedService: Executing scheduled morning push...");
                await ExecutePushAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MahaITDailyDashboardPushHostedService: Error during scheduled push. Retrying in 30 minutes.");
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        _logger.LogInformation("MahaITDailyDashboardPushHostedService stopped.");
    }

    private async Task ExecutePushAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dashboardService = scope.ServiceProvider.GetRequiredService<IMahaITDashboardService>();

            var now = DateTime.Now;
            int currentYear = now.Year;
            int currentMonth = now.Month;

            // 1. If today is the 1st or 2nd day of the month, also push previous month's finalized summary
            if (now.Day <= 2)
            {
                var prevMonthDate = now.AddMonths(-1);
                _logger.LogInformation("MahaITDailyDashboardPushHostedService: Pushing finalized summary for previous month ({Year}-{Month})",
                    prevMonthDate.Year, prevMonthDate.Month);

                var prevResult = await dashboardService.PushToMahaITDashboardAsync(prevMonthDate.Year, prevMonthDate.Month, ct);
                _logger.LogInformation("MahaITDailyDashboardPushHostedService: Previous month push result: IsSuccess={IsSuccess}, Message={Message}",
                    prevResult.IsSuccess, prevResult.Message);
            }

            // 2. Push current month's latest summary up to today
            _logger.LogInformation("MahaITDailyDashboardPushHostedService: Pushing current month summary ({Year}-{Month})",
                currentYear, currentMonth);

            var result = await dashboardService.PushToMahaITDashboardAsync(currentYear, currentMonth, ct);
            _logger.LogInformation("MahaITDailyDashboardPushHostedService: Current month push result: IsSuccess={IsSuccess}, Services={SuccessCount}/{TotalCount}, Message={Message}",
                result.IsSuccess, result.SuccessServices, result.TotalServices, result.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MahaITDailyDashboardPushHostedService: Exception executing push to MahaIT dashboard.");
        }
    }
}
