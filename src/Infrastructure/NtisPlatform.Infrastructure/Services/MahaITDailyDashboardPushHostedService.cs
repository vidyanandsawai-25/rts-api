using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Services;

/// <summary>
/// Background service that automatically pushes daily RTS application statistics 
/// to the Government MahaIT RTS Central Dashboard every morning based on configuration 
/// in RTS.AapleSarkarConfiguration (table [RTS].[AapleSarkarCredential]).
/// Document Reference: DashboardAPI_clientDoc_New.pdf
/// </summary>
public class MahaITDailyDashboardPushHostedService : BackgroundService
{
    private readonly ILogger<MahaITDailyDashboardPushHostedService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public MahaITDailyDashboardPushHostedService(
        ILogger<MahaITDailyDashboardPushHostedService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    private async Task<RTSAapleSarkarConfigurationEntity?> GetConfigurationAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await db.RTSAapleSarkarConfigurations
                .AsNoTracking()
                .OrderByDescending(c => c.Id)
                .FirstOrDefaultAsync(c => c.IsActive, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MahaITDailyDashboardPushHostedService: Failed to retrieve RTS.AapleSarkarConfiguration from database.");
            return null;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MahaITDailyDashboardPushHostedService started.");

        // Wait on startup to ensure database connections and migrations are ready
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        // Check if run on startup is configured in RTS.AapleSarkarConfiguration
        var startupConfig = await GetConfigurationAsync(stoppingToken);
        if (startupConfig != null && startupConfig.RunOnStartup && startupConfig.DailyPushEnabled)
        {
            _logger.LogInformation("MahaITDailyDashboardPushHostedService: RunOnStartup is enabled in RTS.AapleSarkarConfiguration. Executing initial push...");
            await ExecutePushAsync(stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var config = await GetConfigurationAsync(stoppingToken);

                bool isEnabled = config?.DailyPushEnabled ?? true;
                int targetHour = config?.MorningPushHour ?? 6;
                int targetMinute = config?.MorningPushMinute ?? 0;

                if (!isEnabled)
                {
                    _logger.LogInformation("MahaITDailyDashboardPushHostedService: Daily push is currently disabled in RTS.AapleSarkarConfiguration. Checking again in 30 minutes.");
                    await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
                    continue;
                }

                var now = DateTime.Now;
                var nextRun = new DateTime(now.Year, now.Month, now.Day, targetHour, targetMinute, 0);

                if (now >= nextRun)
                {
                    nextRun = nextRun.AddDays(1);
                }

                var delay = nextRun - now;
                _logger.LogInformation("MahaITDailyDashboardPushHostedService: Next scheduled morning push is at {NextRun} (waiting {Delay:hh\\:mm\\:ss}) based on RTS.AapleSarkarConfiguration [Push Time: {Hour:D2}:{Minute:D2}]", 
                    nextRun, delay, targetHour, targetMinute);

                await Task.Delay(delay, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                    break;

                // Re-verify that push is still enabled in the database before executing
                var currentConfig = await GetConfigurationAsync(stoppingToken);
                if (currentConfig != null && !currentConfig.DailyPushEnabled)
                {
                    _logger.LogInformation("MahaITDailyDashboardPushHostedService: Daily push was disabled in RTS.AapleSarkarConfiguration before scheduled run. Skipping execution.");
                    continue;
                }

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
