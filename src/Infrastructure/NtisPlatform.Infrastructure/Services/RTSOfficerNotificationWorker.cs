using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Options;
using NtisPlatform.Core.Entities;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Services;

/// <summary>
/// Autonomous background worker for RTS approval officers.
/// Monitors stage transitions, dispatches real-time WhatsApp + SMS desk alerts,
/// and computes daily SLA breach warnings without touching the core RTS API flow.
/// Runs as a hosted service inside the Web API and/or Worker process.
/// </summary>
public class RTSOfficerNotificationWorker : BackgroundService
{
    private readonly ILogger<RTSOfficerNotificationWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<WhatsAppOptions> _options;
    private DateTime _lastDigestDate = DateTime.MinValue;

    public RTSOfficerNotificationWorker(
        ILogger<RTSOfficerNotificationWorker> logger,
        IServiceScopeFactory scopeFactory,
        IOptions<WhatsAppOptions> options)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _options = options;
    }

    private static int ParseSlaDays(string? raw, int defaultDays = 15)
    {
        if (string.IsNullOrWhiteSpace(raw)) return defaultDays;
        var digits = new string(raw.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var parsed) && parsed > 0 ? parsed : defaultDays;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RTS Officer Notification Background Worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var workerConfig = _options.Value.Worker;
            if (!workerConfig.IsEnabled || !_options.Value.IsActive)
            {
                _logger.LogDebug("RTS Officer Notification Worker is disabled in configuration. Waiting 5 minutes...");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                continue;
            }

            try
            {
                // 1. Process recent stage assignments (desk alerts)
                await ProcessNewStageAssignmentsAsync(stoppingToken);

                // 2. Process SLA breach warnings (urgent & overdue applications)
                await ProcessSlaWarningsAsync(stoppingToken);

                // 3. Process daily morning officer digest (runs around 9:30 AM IST)
                await ProcessDailyDigestAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during RTS Officer Notification processing loop.");
            }

            var pollInterval = TimeSpan.FromMinutes(Math.Max(1, workerConfig.PollIntervalMinutes));
            await Task.Delay(pollInterval, stoppingToken);
        }

        _logger.LogInformation("RTS Officer Notification Background Worker stopped.");
    }

    /// <summary>
    /// Scans recent stage allocations and dispatches WhatsApp + SMS to the newly assigned officer.
    /// Checks CORE.SMSSendDetails to guarantee zero duplicate dispatches.
    /// </summary>
    private async Task ProcessNewStageAssignmentsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var waService = scope.ServiceProvider.GetRequiredService<IRTSWhatsAppNotificationService>();
        var rtsSmsService = scope.ServiceProvider.GetService<IRTSSmsNotificationService>();

        var templateName = _options.Value.Templates.OfficerAssigned;
        var portalUrl = _options.Value.Worker.OfficerPortalBaseUrl;

        // Fetch applications currently active at a stage with an assigned officer
        var activeApplications = await db.RTSApplicationDetails
            .AsNoTracking()
            .Include(a => a.Service)
            .Include(a => a.User)
            .Where(a => a.CurrentApprovalFlowStageId > 0
                     && a.UserId.HasValue
                     && a.ApplicationStatus != "APPROVED"
                     && a.ApplicationStatus != "REJECTED"
                     && a.ApplicationStatus != "DELETED"
                     && !a.MarkedForDeletion)
            .Select(a => new
            {
                ApplicationId = a.Id,
                ApplicationNo = a.ApplicationNo,
                ServiceName = a.Service.ServiceName,
                StageId = a.CurrentApprovalFlowStageId,
                OfficerUserId = a.UserId.Value,
                OfficerName = ((a.User.FirstName ?? "") + " " + (a.User.LastName ?? "")).Trim(),
                OfficerMobile = a.User.MobileNo,
                CreatedDate = a.CreatedDate,
                SlaDaysRaw = a.Service.Sla
            })
            .ToListAsync(ct);

        if (!activeApplications.Any()) return;

        var appIds = activeApplications.Select(x => x.ApplicationId).ToList();

        // Check which application + stage combinations have already received the stage assignment notification
        var alreadySentStageAlerts = await db.SMSSendDetails
            .AsNoTracking()
            .Where(s => s.ApplicationId.HasValue
                     && appIds.Contains(s.ApplicationId.Value)
                     && (s.TemplateID == templateName || s.TemplateID == "1777179560503279488"))
            .Select(s => new { AppId = s.ApplicationId!.Value, StageId = s.SMSTypeID })
            .ToListAsync(ct);

        var pendingAlerts = activeApplications
            .Where(a => !alreadySentStageAlerts.Any(s => s.AppId == a.ApplicationId && s.StageId == a.StageId))
            .ToList();

        foreach (var item in pendingAlerts)
        {
            if (string.IsNullOrWhiteSpace(item.OfficerMobile))
            {
                _logger.LogWarning("Skipping officer alert for Application {AppNo}: Assigned officer {UserId} has no mobile number.",
                    item.ApplicationNo, item.OfficerUserId);
                continue;
            }

            var officerName = string.IsNullOrWhiteSpace(item.OfficerName) ? "Officer" : item.OfficerName;
            var slaDays = ParseSlaDays(item.SlaDaysRaw, 15);
            var dueDate = (item.CreatedDate ?? DateTime.UtcNow).AddDays(slaDays).ToString("dd-MM-yyyy");

            // Look up stage name if available
            var stageName = await db.Set<RTSApprovalFlowStageMasterEntity>()
                .AsNoTracking()
                .Where(s => s.Id == item.StageId)
                .Select(s => s.StageName)
                .FirstOrDefaultAsync(ct) ?? "Document Verification / Review";

            _logger.LogInformation("Dispatching task assignment notice for Application {AppNo} to Officer {Officer} ({Mobile})...",
                item.ApplicationNo, officerName, item.OfficerMobile);

            // 1. Dispatch WhatsApp (Primary rich card)
            await waService.SendOfficerTaskAssignedAsync(
                item.OfficerMobile,
                officerName,
                item.ApplicationNo,
                item.ServiceName,
                stageName,
                dueDate,
                portalUrl,
                applicationId: item.ApplicationId,
                stageId: item.StageId,
                officerUserId: item.OfficerUserId,
                ct: ct);

            // 2. Dual-channel: Also dispatch official SMS (100% dynamic from CORE.SMSMaster)
            if (rtsSmsService != null)
            {
                try
                {
                    await rtsSmsService.SendOfficerAssignedSmsAsync(
                        item.OfficerMobile,
                        officerName,
                        item.ApplicationNo,
                        item.ServiceName,
                        dueDate,
                        item.ApplicationId,
                        ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Dual-channel SMS dispatch skipped for {Mobile}", item.OfficerMobile);
                }
            }
        }
    }

    /// <summary>
    /// Evaluates applications near SLA breach (<= thresholdDays remaining) or already overdue.
    /// Dispatches at most ONE warning alert per application per calendar day to avoid spamming officers.
    /// </summary>
    private async Task ProcessSlaWarningsAsync(CancellationToken ct)
    {
        var config = _options.Value.Worker;
        var thresholdDays = Math.Max(1, config.SlaWarningThresholdDays);
        var slaTemplate = _options.Value.Templates.OfficerSlaWarning;
        var portalUrl = config.OfficerPortalBaseUrl;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var waService = scope.ServiceProvider.GetRequiredService<IRTSWhatsAppNotificationService>();
        var rtsSmsService = scope.ServiceProvider.GetService<IRTSSmsNotificationService>();

        var activeApps = await db.RTSApplicationDetails
            .AsNoTracking()
            .Include(a => a.Service)
            .Include(a => a.User)
            .Where(a => a.CurrentApprovalFlowStageId > 0
                     && a.UserId.HasValue
                     && a.ApplicationStatus != "APPROVED"
                     && a.ApplicationStatus != "REJECTED"
                     && a.ApplicationStatus != "DELETED"
                     && !a.MarkedForDeletion)
            .Select(a => new
            {
                ApplicationId = a.Id,
                ApplicationNo = a.ApplicationNo,
                ServiceName = a.Service.ServiceName,
                OfficerUserId = a.UserId.Value,
                OfficerName = ((a.User.FirstName ?? "") + " " + (a.User.LastName ?? "")).Trim(),
                OfficerMobile = a.User.MobileNo,
                CreatedDate = a.CreatedDate,
                SlaDaysRaw = a.Service.Sla
            })
            .ToListAsync(ct);

        if (!activeApps.Any()) return;

        var todayUtc = DateTime.UtcNow.Date;

        foreach (var app in activeApps)
        {
            if (string.IsNullOrWhiteSpace(app.OfficerMobile)) continue;

            var slaDays = ParseSlaDays(app.SlaDaysRaw, 15);
            var dueDateObj = (app.CreatedDate ?? DateTime.UtcNow).AddDays(slaDays);
            var remainingTotalDays = (dueDateObj - DateTime.UtcNow).TotalDays;

            // Only trigger if within threshold or already overdue
            if (remainingTotalDays > thresholdDays) continue;

            var daysRemaining = Math.Max(0, (int)Math.Ceiling(remainingTotalDays));

            // Check if already warned today via WhatsApp or DLT SMS
            var alreadyWarnedToday = await db.SMSSendDetails
                .AsNoTracking()
                .AnyAsync(s => s.ApplicationId == app.ApplicationId
                            && (s.TemplateID == slaTemplate || s.TemplateID == "1777179560524049216")
                            && s.CreatedDate >= todayUtc, ct);

            if (alreadyWarnedToday) continue;

            var officerName = string.IsNullOrWhiteSpace(app.OfficerName) ? "Officer" : app.OfficerName;

            _logger.LogInformation("Sending SLA Warning for Application {AppNo} (breaches in {Days} days) to Officer {Officer} ({Mobile})...",
                app.ApplicationNo, daysRemaining, officerName, app.OfficerMobile);

            // 1. WhatsApp warning card
            await waService.SendOfficerSlaWarningAsync(
                app.OfficerMobile,
                officerName,
                app.ApplicationNo,
                app.ServiceName,
                daysRemaining,
                dueDateObj.ToString("dd-MM-yyyy"),
                portalUrl,
                applicationId: app.ApplicationId,
                officerUserId: app.OfficerUserId,
                ct: ct);

            // 2. Official DLT SMS
            if (rtsSmsService != null)
            {
                try
                {
                    await rtsSmsService.SendOfficerSlaWarningSmsAsync(
                        app.OfficerMobile,
                        app.ApplicationNo,
                        app.ServiceName,
                        daysRemaining,
                        dueDateObj.ToString("dd-MM-yyyy"),
                        app.ApplicationId,
                        ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "SLA warning SMS skipped for {Mobile}", app.OfficerMobile);
                }
            }
        }
    }

    /// <summary>
    /// Runs daily at the configured morning time (default 9:30 AM IST).
    /// Sends an aggregated officer daily pending digest.
    /// </summary>
    private async Task ProcessDailyDigestAsync(CancellationToken ct)
    {
        var config = _options.Value.Worker;
        var now = DateTime.UtcNow.AddHours(5.5); // Indian Standard Time (IST)

        // Check if already executed today
        if (_lastDigestDate.Date == now.Date) return;

        // Check if current IST time has reached the scheduled morning digest time
        if (now.Hour < config.MorningDigestHour || (now.Hour == config.MorningDigestHour && now.Minute < config.MorningDigestMinute))
        {
            return;
        }

        _logger.LogInformation("Starting Daily Morning RTS Officer Digest run for date: {Date}...", now.ToString("yyyy-MM-dd"));

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var waService = scope.ServiceProvider.GetRequiredService<IRTSWhatsAppNotificationService>();

        var digestTemplate = _options.Value.Templates.OfficerDailyDigest;
        var portalUrl = config.OfficerPortalBaseUrl;
        var thresholdDays = Math.Max(1, config.SlaWarningThresholdDays);

        var activeApps = await db.RTSApplicationDetails
            .AsNoTracking()
            .Include(a => a.Service)
            .Include(a => a.User)
            .Where(a => a.CurrentApprovalFlowStageId > 0
                     && a.UserId.HasValue
                     && a.ApplicationStatus != "APPROVED"
                     && a.ApplicationStatus != "REJECTED"
                     && a.ApplicationStatus != "DELETED"
                     && !a.MarkedForDeletion)
            .Select(a => new
            {
                ApplicationId = a.Id,
                OfficerUserId = a.UserId.Value,
                OfficerName = ((a.User.FirstName ?? "") + " " + (a.User.LastName ?? "")).Trim(),
                OfficerMobile = a.User.MobileNo,
                CreatedDate = a.CreatedDate,
                SlaDaysRaw = a.Service.Sla
            })
            .ToListAsync(ct);

        if (!activeApps.Any())
        {
            _lastDigestDate = now;
            return;
        }

        // Group applications by Officer
        var officerGroups = activeApps.GroupBy(a => a.OfficerUserId).ToList();

        foreach (var group in officerGroups)
        {
            var first = group.First();
            if (string.IsNullOrWhiteSpace(first.OfficerMobile)) continue;

            var officerName = string.IsNullOrWhiteSpace(first.OfficerName) ? "Officer" : first.OfficerName;
            var totalPending = group.Count();
            var nearSlaCount = group.Count(a =>
            {
                var sla = ParseSlaDays(a.SlaDaysRaw, 15);
                var due = (a.CreatedDate ?? DateTime.UtcNow).AddDays(sla);
                var daysRemaining = (due - DateTime.UtcNow).TotalDays;
                return daysRemaining <= thresholdDays;
            });

            // Send Daily Pending Digest
            var alreadyDigestedToday = await db.SMSSendDetails
                .AsNoTracking()
                .AnyAsync(s => s.ReceiverMobileNo == first.OfficerMobile
                            && s.TemplateID == digestTemplate
                            && s.CreatedDate >= DateTime.UtcNow.Date, ct);

            if (!alreadyDigestedToday && totalPending > 0)
            {
                _logger.LogInformation("Sending Daily Digest to Officer {Officer} ({Total} pending, {Near} near SLA)...",
                    officerName, totalPending, nearSlaCount);

                await waService.SendOfficerDailyDigestAsync(
                    first.OfficerMobile,
                    officerName,
                    totalPending,
                    nearSlaCount,
                    portalUrl,
                    officerUserId: first.OfficerUserId,
                    ct: ct);
            }
        }

        _lastDigestDate = now;
        _logger.LogInformation("Completed Daily Morning RTS Officer Digest run.");
    }
}
