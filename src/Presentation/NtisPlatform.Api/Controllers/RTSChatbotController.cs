using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Options;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Dedicated Webhook & API Controller for the RTS Citizen WhatsApp Chatbot.
/// Connects directly to the live RTS SQL Server database to provide real-time
/// application tracking, citizen profiling, service catalog browsing, fee payment,
/// and digital certificate downloads without touching legacy code.
/// </summary>
[Route("api/rts-chatbot")]
[ApiController]
[AllowAnonymous]
public class RTSChatbotController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IRTSWhatsAppNotificationService _waService;
    private readonly IRTSSmsNotificationService _smsService;
    private readonly IOptions<WhatsAppOptions> _waOptions;
    private readonly ILogger<RTSChatbotController> _logger;

    public RTSChatbotController(
        ApplicationDbContext db,
        IRTSWhatsAppNotificationService waService,
        IRTSSmsNotificationService smsService,
        IOptions<WhatsAppOptions> waOptions,
        ILogger<RTSChatbotController> logger)
    {
        _db = db;
        _waService = waService;
        _smsService = smsService;
        _waOptions = waOptions;
        _logger = logger;
    }

    private static int ParseSlaDays(string? raw, int defaultDays = 15)
    {
        if (string.IsNullOrWhiteSpace(raw)) return defaultDays;
        var digits = new string(raw.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var parsed) && parsed > 0 ? parsed : defaultDays;
    }

    /// <summary>
    /// 1. Real-time Application Tracking for Chatbot
    /// Returns live status, current officer, SLA timeline, pending fees, and certificate URL.
    /// </summary>
    [HttpGet("track/{applicationNo}")]
    public async Task<IActionResult> TrackApplication(string applicationNo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(applicationNo))
        {
            return BadRequest(new { success = false, message = "Application number is required." });
        }

        var cleanAppNo = applicationNo.Trim();

        var app = await _db.RTSApplicationDetails
            .AsNoTracking()
            .Include(a => a.Service)
            .Include(a => a.Department)
            .Include(a => a.User)
            .Include(a => a.TrackApplicationHistory)
            .FirstOrDefaultAsync(a => a.ApplicationNo == cleanAppNo || a.Id.ToString() == cleanAppNo, ct);

        if (app == null)
        {
            return NotFound(new { success = false, message = $"Application '{cleanAppNo}' not found." });
        }

        var slaDays = ParseSlaDays(app.Service?.Sla, 15);
        var createdDate = app.CreatedDate ?? DateTime.UtcNow;
        var dueDate = createdDate.AddDays(slaDays);
        var remainingDaysRaw = (dueDate - DateTime.UtcNow).TotalDays;
        var daysRemaining = Math.Max(0, (int)Math.Ceiling(remainingDaysRaw));
        var isOverdue = remainingDaysRaw < 0 && app.ApplicationStatus != "APPROVED" && app.ApplicationStatus != "REJECTED";

        // Current stage name
        string currentStageName = "Desk Review";
        if (app.CurrentApprovalFlowStageId > 0)
        {
            var stage = await _db.Set<RTSApprovalFlowStageMasterEntity>()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == app.CurrentApprovalFlowStageId, ct);
            if (stage != null) currentStageName = stage.StageName;
        }

        // Check if statutory payment is pending
        var pendingDemand = await _db.RTSPaymentDemands
            .AsNoTracking()
            .Where(d => d.ApplicationId == app.Id && d.DemandStatus == "Pending" && d.IsActive)
            .OrderByDescending(d => d.Id)
            .FirstOrDefaultAsync(ct);

        var portalBaseUrl = _waOptions.Value.Worker.OfficerPortalBaseUrl.Replace("/officer", "/service");

        var response = new
        {
            success = true,
            applicationId = app.Id,
            applicationNo = app.ApplicationNo,
            applicantName = string.IsNullOrWhiteSpace(app.ApplicantName) ? "Citizen" : app.ApplicantName,
            serviceName = app.Service?.ServiceName ?? "RTS Service",
            departmentName = app.Department?.DepartmentName ?? "Municipal Department",
            status = app.ApplicationStatus,
            currentStage = currentStageName,
            assignedOfficer = app.User != null ? ((app.User.FirstName ?? "") + " " + (app.User.LastName ?? "")).Trim() : "Reviewing Officer",
            createdDate = createdDate.ToString("dd-MM-yyyy"),
            dueDate = dueDate.ToString("dd-MM-yyyy"),
            slaDays = slaDays,
            daysRemaining = daysRemaining,
            isOverdue = isOverdue,
            remark = app.Remark,
            isPaymentPending = pendingDemand != null && pendingDemand.TotalAmount > 0,
            pendingAmount = pendingDemand?.TotalAmount ?? 0,
            paymentUrl = pendingDemand != null ? $"{portalBaseUrl}/payment?appId={app.Id}" : null,
            isCertificateIssued = app.IssuedCertificateGuid.HasValue,
            certificateGuid = app.IssuedCertificateGuid,
            certificateUrl = app.IssuedCertificateGuid.HasValue
                ? $"{portalBaseUrl}/verify-certificate/{app.IssuedCertificateGuid.Value}"
                : null
        };

        return Ok(response);
    }

    /// <summary>
    /// 2. Citizen Active Applications by Mobile Number
    /// Automatically authenticates citizen via their WhatsApp mobile number.
    /// </summary>
    [HttpGet("citizen-overview")]
    public async Task<IActionResult> GetCitizenOverview([FromQuery] string mobile, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(mobile))
        {
            return BadRequest(new { success = false, message = "Mobile number is required." });
        }

        var digits = new string(mobile.Where(char.IsDigit).ToArray());
        if (digits.Length > 10 && digits.StartsWith("91")) digits = digits.Substring(2);

        var apps = await _db.RTSApplicationDetails
            .AsNoTracking()
            .Include(a => a.Service)
            .Include(a => a.Department)
            .Where(a => (a.ApplicantMobileNo == digits || a.ApplicantMobileNo == "91" + digits || a.ApplicantMobileNo == "+91" + digits)
                     && !a.MarkedForDeletion)
            .OrderByDescending(a => a.Id)
            .Take(5)
            .Select(a => new
            {
                applicationId = a.Id,
                applicationNo = a.ApplicationNo,
                serviceName = a.Service.ServiceName,
                departmentName = a.Department.DepartmentName,
                status = a.ApplicationStatus,
                createdDate = a.CreatedDate.HasValue ? a.CreatedDate.Value.ToString("dd-MM-yyyy") : "",
                hasCertificate = a.IssuedCertificateGuid.HasValue,
                certificateGuid = a.IssuedCertificateGuid
            })
            .ToListAsync(ct);

        return Ok(new
        {
            success = true,
            mobile = digits,
            totalApplications = apps.Count,
            applications = apps
        });
    }

    /// <summary>
    /// 3. RTS Departments List for Interactive Catalog
    /// </summary>
    [HttpGet("departments")]
    public async Task<IActionResult> GetDepartments(CancellationToken ct)
    {
        var depts = await _db.Set<RTSDepartmentEntity>()
            .AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.DisplayOrder)
            .Select(d => new
            {
                departmentId = d.Id,
                nameEn = d.DepartmentName,
                nameMr = d.DepartmentNameLocal
            })
            .ToListAsync(ct);

        return Ok(new { success = true, departments = depts });
    }

    /// <summary>
    /// 4. RTS Services by Department with SLA Days
    /// </summary>
    [HttpGet("departments/{departmentId:int}/services")]
    public async Task<IActionResult> GetServicesByDepartment(int departmentId, CancellationToken ct)
    {
        var services = await _db.RTSServices
            .AsNoTracking()
            .Where(s => s.DepartmentId == departmentId && s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .Select(s => new
            {
                serviceId = s.Id,
                serviceNameEn = s.ServiceName,
                serviceNameMr = s.ServiceNameLocal,
                sla = s.Sla,
                fees = s.Fees,
                feesRequired = s.FeesRequired
            })
            .ToListAsync(ct);

        return Ok(new { success = true, departmentId, services });
    }
}
