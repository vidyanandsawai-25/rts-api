using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.RTSAppeal;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Domain service implementation governing Right to Services (RTS) First and Second Appeal adjudication.
/// Enforces Maharashtra Right to Services Act (MRTSA 2015) statutory timelines, deemed-delay calculations, and domain isolation.
/// </summary>
public class RTSAppealService : IRTSAppealService, IRtsAppealService
{
    private readonly IRepository<RTSAppealApplicationEntity, int> _appealRepository;
    private readonly IRepository<RTSAppealTypeMasterEntity, int> _appealTypeRepository;
    private readonly IRepository<RTSAppealFlowStageMasterEntity, int> _appealStageRepository;
    private readonly IRepository<RTSTrackAppealHistoryEntity, int> _appealHistoryRepository;
    private readonly IRepository<RTSApplicationDetailsEntity, int> _applicationRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RTSAppealService>? _logger;

    public RTSAppealService(
        IRepository<RTSAppealApplicationEntity, int> appealRepository,
        IRepository<RTSAppealTypeMasterEntity, int> appealTypeRepository,
        IRepository<RTSAppealFlowStageMasterEntity, int> appealStageRepository,
        IRepository<RTSTrackAppealHistoryEntity, int> appealHistoryRepository,
        IRepository<RTSApplicationDetailsEntity, int> applicationRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<RTSAppealService>? logger = null)
    {
        _appealRepository = appealRepository;
        _appealTypeRepository = appealTypeRepository;
        _appealStageRepository = appealStageRepository;
        _appealHistoryRepository = appealHistoryRepository;
        _applicationRepository = applicationRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Parses the service SLA string (e.g., "7 Days", "15", "0 Days") into integer days.
    /// </summary>
    public static int ParseServiceSlaDays(string? slaString, string serviceName)
    {
        if (string.IsNullOrWhiteSpace(slaString)) return 7; // Default statutory SLA fallback

        var match = Regex.Match(slaString, @"\d+");
        if (match.Success && int.TryParse(match.Value, out int days) && days > 0)
        {
            return days;
        }

        return 7;
    }

    /// <inheritdoc />
    public async Task<List<RTSAppealTypeDto>> GetAppealTypesAsync(CancellationToken ct = default)
    {
        return await _appealTypeRepository.GetQueryable()
            .Select(t => new RTSAppealTypeDto
            {
                Id = t.Id,
                Code = t.Code,
                AppealTypeName = t.AppealTypeName
            })
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<RTSAppealLevelCheckResultDto> CheckAppealLevelAsync(string applicationNo, CancellationToken ct = default)
    {
        var app = await _applicationRepository.GetQueryable()
            .Include(a => a.Service)
            .Include(a => a.TrackApplicationHistory)
            .FirstOrDefaultAsync(a => a.ApplicationNo == applicationNo && a.IsActive && !a.MarkedForDeletion, ct);

        if (app == null)
        {
            return new RTSAppealLevelCheckResultDto
            {
                ApplicationNo = applicationNo,
                CanFileAppeal = false,
                BlockReason = $"Application #{applicationNo} does not exist in the system."
            };
        }

        var existingAppeals = await _appealRepository.GetQueryable()
            .Where(a => a.ApplicationId == app.Id && a.IsActive && !a.MarkedForDeletion)
            .OrderBy(a => a.AppealLevel)
            .ToListAsync(ct);

        var firstAppeal = existingAppeals.FirstOrDefault(a => a.AppealLevel == 1);
        var secondAppeal = existingAppeals.FirstOrDefault(a => a.AppealLevel == 2);

        // Rule: If second appeal already exists, no further appeals are permitted under MRTSA 2015
        if (secondAppeal != null)
        {
            return new RTSAppealLevelCheckResultDto
            {
                ApplicationNo = applicationNo,
                AppealLevel = "2nd Appeal",
                IsSecondAppeal = true,
                ExistingAppealsCount = existingAppeals.Count,
                CanFileAppeal = false,
                BlockReason = $"Second Appeal #{secondAppeal.AppealNo} has already been filed ({secondAppeal.AppealStatus}). No further appeals are permitted under the Right to Services Act."
            };
        }

        // Determine if citizen is filing First Appeal or Second Appeal
        bool isSecondAppeal = firstAppeal != null;
        string appealLevelStr = isSecondAppeal ? "2nd Appeal" : "1st Appeal";
        string suggestedAppealNo = $"RTS/{DateTime.Now.Year}/{app.ApplicationNo}-A{(isSecondAppeal ? 2 : 1)}";

        // Second Appeal: Condition check on First Appeal status
        if (isSecondAppeal)
        {
            if (string.Equals(firstAppeal!.AppealStatus, "Pending", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(firstAppeal.AppealStatus, "Submitted", StringComparison.OrdinalIgnoreCase))
            {
                return new RTSAppealLevelCheckResultDto
                {
                    ApplicationNo = applicationNo,
                    AppealLevel = appealLevelStr,
                    IsSecondAppeal = true,
                    ExistingAppealsCount = existingAppeals.Count,
                    FirstAppealNo = firstAppeal.AppealNo,
                    FirstAppealStatus = firstAppeal.AppealStatus,
                    CanFileAppeal = false,
                    BlockReason = $"First Appeal #{firstAppeal.AppealNo} is currently Pending review by the First Appellate Authority. Second Appeal can only be filed after the First Appeal has been adjudicated or if the statutory 30-day decision period has expired."
                };
            }

            // Trigger date for Second Appeal is either First Appeal order date or expiry of 30 days
            DateTime triggerDate = firstAppeal.ActionDate ?? firstAppeal.CreatedDate?.AddDays(30) ?? DateTime.Now;
            int elapsedDays = (int)(DateTime.Now.Date - triggerDate.Date).TotalDays;

            string filingCategory = elapsedDays <= 30 ? "Normal" : "Delayed";
            bool requiresDelayJustification = elapsedDays > 30;

            if (elapsedDays > 90)
            {
                return new RTSAppealLevelCheckResultDto
                {
                    ApplicationNo = applicationNo,
                    AppealLevel = appealLevelStr,
                    IsSecondAppeal = true,
                    ExistingAppealsCount = existingAppeals.Count,
                    FirstAppealNo = firstAppeal.AppealNo,
                    FirstAppealStatus = firstAppeal.AppealStatus,
                    CanFileAppeal = false,
                    TriggerDate = triggerDate,
                    TriggerReason = $"First Appeal Order Date: {triggerDate:dd-MM-yyyy}",
                    ElapsedDays = elapsedDays,
                    BlockReason = $"The statutory period of 90 days for filing a Second Appeal has expired ({elapsedDays} days elapsed). Under MRTSA 2015, appeals beyond 90 days cannot be entertained."
                };
            }

            return new RTSAppealLevelCheckResultDto
            {
                ApplicationNo = applicationNo,
                AppealLevel = appealLevelStr,
                IsSecondAppeal = true,
                ExistingAppealsCount = existingAppeals.Count,
                SuggestedAppealNo = suggestedAppealNo,
                FirstAppealNo = firstAppeal.AppealNo,
                FirstAppealStatus = firstAppeal.AppealStatus,
                CanFileAppeal = true,
                TriggerDate = triggerDate,
                TriggerReason = $"First Appeal Order Date ({firstAppeal.AppealStatus}): {triggerDate:dd-MM-yyyy}",
                ElapsedDays = elapsedDays,
                FilingCategory = filingCategory,
                RequiresDelayJustification = requiresDelayJustification
            };
        }

        // First Appeal: Determine grievance trigger (Rejection vs Deemed Delay vs SLA breach)
        bool isRejected = string.Equals(app.ApplicationStatus, "Rejected", StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(app.ApplicationStatus, "Reject", StringComparison.OrdinalIgnoreCase);

        DateTime? rejectionDate = null;
        if (isRejected)
        {
            var rejectHistory = app.TrackApplicationHistory?
                .Where(h => string.Equals(h.Status, "Rejected", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(h.Action, "Rejected", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(h => h.CreatedDate)
                .FirstOrDefault();

            rejectionDate = rejectHistory?.CreatedDate ?? app.UpdatedDate ?? app.CreatedDate;
        }

        int slaDays = ParseServiceSlaDays(app.Service?.Sla, app.Service?.ServiceName ?? "");
        DateTime applicationCreatedDate = app.CreatedDate ?? DateTime.Now;
        DateTime slaExpiryDate = applicationCreatedDate.AddDays(slaDays);
        DateTime deemedDelayDate = applicationCreatedDate.AddDays(45); // MRTSA Section 8 statutory deemed delay

        DateTime triggerDateVal;
        string triggerReasonStr;

        if (isRejected && rejectionDate.HasValue)
        {
            triggerDateVal = rejectionDate.Value;
            triggerReasonStr = $"Application Rejected on {triggerDateVal:dd-MM-yyyy}";
        }
        else if (DateTime.Now >= slaExpiryDate)
        {
            triggerDateVal = slaExpiryDate;
            triggerReasonStr = $"SLA Breach: Service SLA of {slaDays} days expired on {slaExpiryDate:dd-MM-yyyy}";
        }
        else if (DateTime.Now >= deemedDelayDate)
        {
            triggerDateVal = deemedDelayDate;
            triggerReasonStr = $"Deemed Delay: Statutory 45-day decision period completed on {deemedDelayDate:dd-MM-yyyy}";
        }
        else
        {
            int remainingDays = (int)(slaExpiryDate.Date - DateTime.Now.Date).TotalDays;
            return new RTSAppealLevelCheckResultDto
            {
                ApplicationNo = applicationNo,
                AppealLevel = appealLevelStr,
                IsSecondAppeal = false,
                ExistingAppealsCount = existingAppeals.Count,
                CanFileAppeal = false,
                ServiceSlaDays = slaDays,
                ServiceSlaExpiryDate = slaExpiryDate,
                DeemedDelayDate = deemedDelayDate,
                BlockReason = $"Application is currently within the active SLA processing period ({remainingDays} days remaining until {slaExpiryDate:dd-MM-yyyy}). An appeal can only be filed if the application is rejected or if the SLA period expires."
            };
        }

        int elapsed = (int)(DateTime.Now.Date - triggerDateVal.Date).TotalDays;
        string category = elapsed <= 30 ? "Normal" : "Delayed";
        bool delayJustification = elapsed > 30;

        if (elapsed > 90)
        {
            return new RTSAppealLevelCheckResultDto
            {
                ApplicationNo = applicationNo,
                AppealLevel = appealLevelStr,
                IsSecondAppeal = false,
                ExistingAppealsCount = existingAppeals.Count,
                CanFileAppeal = false,
                TriggerDate = triggerDateVal,
                TriggerReason = triggerReasonStr,
                ElapsedDays = elapsed,
                ServiceSlaDays = slaDays,
                ServiceSlaExpiryDate = slaExpiryDate,
                DeemedDelayDate = deemedDelayDate,
                BlockReason = $"The statutory period of 90 days for filing a First Appeal has expired ({elapsed} days elapsed since trigger). Under MRTSA 2015, appeals beyond 90 days cannot be entertained."
            };
        }

        return new RTSAppealLevelCheckResultDto
        {
            ApplicationNo = applicationNo,
            AppealLevel = appealLevelStr,
            IsSecondAppeal = false,
            ExistingAppealsCount = existingAppeals.Count,
            SuggestedAppealNo = suggestedAppealNo,
            CanFileAppeal = true,
            TriggerDate = triggerDateVal,
            TriggerReason = triggerReasonStr,
            ElapsedDays = elapsed,
            ServiceSlaDays = slaDays,
            ServiceSlaExpiryDate = slaExpiryDate,
            DeemedDelayDate = deemedDelayDate,
            FilingCategory = category,
            RequiresDelayJustification = delayJustification
        };
    }

    /// <inheritdoc />
    public async Task<RTSAppealApplicationSummaryDto?> GetApplicationSummaryForAppealAsync(string applicationNo, CancellationToken ct = default)
    {
        var app = await _applicationRepository.GetQueryable()
            .Include(a => a.Service)
                .ThenInclude(s => s.Department)
            .Include(a => a.Department)
            .Include(a => a.FieldValueData)
                .ThenInclude(f => f.FieldDefinition)
            .Include(a => a.TrackApplicationHistory)
            .FirstOrDefaultAsync(a => a.ApplicationNo == applicationNo && a.IsActive && !a.MarkedForDeletion, ct);

        if (app == null) return null;

        var rejectHistory = app.TrackApplicationHistory?
            .Where(h => string.Equals(h.Status, "Rejected", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(h.Action, "Rejected", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(h => h.CreatedDate)
            .FirstOrDefault();

        string serviceName = !string.IsNullOrWhiteSpace(app.Service?.ServiceName)
            ? app.Service.ServiceName
            : (!string.IsNullOrWhiteSpace(app.Service?.ServiceNameLocal) ? app.Service.ServiceNameLocal : "RTS Service");

        string departmentName = !string.IsNullOrWhiteSpace(app.Department?.DepartmentName)
            ? app.Department.DepartmentName
            : (!string.IsNullOrWhiteSpace(app.Service?.Department?.DepartmentName) ? app.Service.Department.DepartmentName : "Property Tax Department");

        var dynamicFields = app.FieldValueData?
            .Where(f => f.FieldDefinition != null && f.FieldDefinition.FieldGroup != "Document Uploads")
            .OrderBy(f => f.FieldDefinition?.DisplayOrder ?? 0)
            .Select(f => new RTSAppealFieldAnswerDto
            {
                FieldId = f.FieldDefinitionId,
                FieldName = f.FieldDefinition?.FieldCode ?? "",
                FieldLabel = !string.IsNullOrWhiteSpace(f.FieldDefinition?.FieldLabel)
                    ? f.FieldDefinition.FieldLabel
                    : (!string.IsNullOrWhiteSpace(f.FieldDefinition?.FieldLabelLocal) ? f.FieldDefinition.FieldLabelLocal : f.FieldDefinition?.FieldCode ?? ""),
                FieldValue = f.TextValue ?? f.NumberValue?.ToString() ?? f.DateValue?.ToString("yyyy-MM-dd") ?? (f.BooleanValue.HasValue ? f.BooleanValue.Value.ToString() : null),
                DisplayValue = f.TextValue ?? f.NumberValue?.ToString() ?? f.DateValue?.ToString("yyyy-MM-dd") ?? (f.BooleanValue.HasValue ? f.BooleanValue.Value.ToString() : null)
            }).ToList() ?? new List<RTSAppealFieldAnswerDto>();

        var documents = app.FieldValueData?
            .Where(f => f.FieldDefinition != null && f.FieldDefinition.FieldGroup == "Document Uploads")
            .Select(f => new RTSAppealDocumentDto
            {
                DocumentId = f.FieldDefinitionId,
                DocumentName = f.FieldDefinition?.FieldLabel ?? "Document",
                DocumentType = f.FieldDefinition?.FieldType ?? "",
                DocumentPath = f.DocumentGuid.HasValue ? f.DocumentGuid.ToString()! : "",
                IsMandatory = f.FieldDefinition?.IsRequired ?? false
            }).ToList() ?? new List<RTSAppealDocumentDto>();

        var summary = new RTSAppealApplicationSummaryDto
        {
            ApplicationId = app.Id,
            ApplicationNo = app.ApplicationNo,
            ServiceName = serviceName,
            DepartmentName = departmentName,
            ApplicantName = app.ApplicantName ?? "",
            ApplicantMobile = app.ApplicantMobileNo ?? "",
            ApplicantEmail = "",
            Status = app.ApplicationStatus ?? "Pending",
            AppliedDate = app.CreatedDate,
            RejectionDate = rejectHistory?.CreatedDate,
            RejectionReason = rejectHistory?.Remark,
            DynamicFields = dynamicFields,
            Documents = documents
        };

        // Prioritize appeal timeline if appeals exist, otherwise application timeline
        var appealHistories = await _appealHistoryRepository.GetQueryable()
            .Where(h => h.ApplicationId == app.Id && h.IsActive)
            .OrderBy(h => h.CreatedDate)
            .ToListAsync(ct);

        if (appealHistories.Any())
        {
            summary.Timeline = appealHistories.Select(h => new RTSAppealHistoryItemDto
            {
                Id = h.Id,
                StageName = h.Action,
                Action = h.Action,
                ActionByName = $"Officer #{h.ActionByUserId}",
                Status = h.Status,
                Remark = h.Remark,
                ActionDate = h.CreatedDate
            }).ToList();
        }
        else if (app.TrackApplicationHistory != null && app.TrackApplicationHistory.Any())
        {
            summary.Timeline = app.TrackApplicationHistory
                .OrderBy(h => h.CreatedDate)
                .Select(h => new RTSAppealHistoryItemDto
                {
                    Id = h.Id,
                    StageName = null,
                    Action = h.Action,
                    ActionByName = $"User #{h.ActionByUserId}",
                    Status = h.Status,
                    Remark = h.Remark,
                    ActionDate = h.CreatedDate
                }).ToList();
        }

        return summary;
    }

    /// <inheritdoc />
    public async Task<RTSAppealApplicationResultDto> SubmitAppealAsync(CreateRTSAppealApplicationDto dto, CancellationToken ct = default)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));
        if (string.IsNullOrWhiteSpace(dto.ApplicationNo)) throw new ArgumentException("Application number is required.", nameof(dto));
        if (dto.AppealTypeId <= 0) throw new ArgumentException("Please select a valid appeal reason.", nameof(dto));
        if (string.IsNullOrWhiteSpace(dto.ReasonForAppeal)) throw new ArgumentException("Reason for appeal (अपीलाचे कारण) is required.", nameof(dto));

        var app = await _applicationRepository.GetQueryable()
            .FirstOrDefaultAsync(a => a.ApplicationNo == dto.ApplicationNo && a.IsActive && !a.MarkedForDeletion, ct);

        if (app == null)
        {
            throw new KeyNotFoundException($"Application #{dto.ApplicationNo} was not found.");
        }

        int appId = app.Id;

        // Perform statutory level & window evaluation
        var levelCheck = await CheckAppealLevelAsync(dto.ApplicationNo, ct);
        if (!levelCheck.CanFileAppeal)
        {
            throw new InvalidOperationException(levelCheck.BlockReason ?? "Appeal cannot be filed for this application at this time.");
        }

        // Delay condonation check: If filing in 31-90 day window, delay justification is mandatory
        if (levelCheck.RequiresDelayJustification && string.IsNullOrWhiteSpace(dto.DelayJustification))
        {
            throw new InvalidOperationException("This appeal is being filed after the standard 30-day statutory period. A justification for delay (विलंब माफीचे कारण) is mandatory under the Maharashtra Right to Public Services Act 2015.");
        }

        int appealLevel = levelCheck.IsSecondAppeal ? 2 : 1;
        int resolvedCreatedBy = 1;
        try
        {
            int currentUid = _currentUserService.GetCurrentUserId();
            if (currentUid > 0) resolvedCreatedBy = currentUid;
        }
        catch
        {
            resolvedCreatedBy = 1;
        }

        // Resolve appellate stage
        int approvalFlowId = app.ApprovalFlowId;
        RTSAppealFlowStageMasterEntity? appealStage = null;

        if (approvalFlowId > 0)
        {
            appealStage = await _appealStageRepository.GetQueryable()
                .FirstOrDefaultAsync(s => s.AppealFlowId == approvalFlowId && s.StageOrder == appealLevel, ct);
        }

        if (appealStage == null)
        {
            appealStage = await _appealStageRepository.GetQueryable()
                .FirstOrDefaultAsync(s => s.StageOrder == appealLevel, ct);
        }

        // Structured reason containing both grievance ground and condonation if present
        string structuredReason = string.IsNullOrWhiteSpace(dto.DelayJustification)
            ? dto.ReasonForAppeal
            : $"Reason for Appeal:\n{dto.ReasonForAppeal}\n\nReason for Delay / Condonation of Delay (विलंब माफीचे कारण):\n{dto.DelayJustification}";

        if (structuredReason.Length > 1000)
        {
            structuredReason = structuredReason.Substring(0, 1000);
        }

        var appealEntity = new RTSAppealApplicationEntity
        {
            ApplicationId = appId,
            AppealNo = !string.IsNullOrWhiteSpace(dto.AppealNo) ? dto.AppealNo : levelCheck.SuggestedAppealNo,
            AppealLevel = appealLevel,
            AppealTypeId = dto.AppealTypeId,
            ReasonForComplaint = structuredReason,
            MobileNumber = !string.IsNullOrWhiteSpace(dto.MobileNo) ? dto.MobileNo : app.ApplicantMobileNo,
            EmailAddress = dto.Email,
            CreatedDate = DateTime.Now,
            CreatedBy = resolvedCreatedBy,
            AppealStatus = "Pending",
            IsActive = true,
            MarkedForDeletion = false
        };

        // Transactional submission: Ensures Appeal record and initial TrackAppealHistory entry commit together
        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await _appealRepository.AddAsync(appealEntity, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            var historyEntry = new RTSTrackAppealHistoryEntity
            {
                AppealId = appealEntity.Id,
                ApplicationId = appId,
                AppealLevel = appealLevel,
                ApprovalFlowId = approvalFlowId > 0 ? approvalFlowId : null,
                AppealFlowStageId = appealStage?.Id,
                ActionByUserId = resolvedCreatedBy,
                Status = $"{levelCheck.AppealLevel} Submitted",
                Action = $"{levelCheck.AppealLevel} Filed ({appealEntity.AppealNo})",
                Remark = dto.ReasonForAppeal.Length > 1000 ? dto.ReasonForAppeal.Substring(0, 1000) : dto.ReasonForAppeal,
                IsActive = true,
                CreatedDate = DateTime.Now
            };
            await _appealHistoryRepository.AddAsync(historyEntry, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            await _unitOfWork.CommitTransactionAsync(ct);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }

        _logger?.LogInformation("RTS Appeal {AppealNo} submitted successfully for Application {AppNo} at level {Level}",
            appealEntity.AppealNo, app.ApplicationNo, appealLevel);

        return new RTSAppealApplicationResultDto
        {
            Success = true,
            Message = $"{levelCheck.AppealLevel} filed successfully with Appeal No: {appealEntity.AppealNo}",
            AppealNo = appealEntity.AppealNo,
            AppealLevel = levelCheck.AppealLevel
        };
    }

    /// <inheritdoc />
    public async Task<RTSAppealDashboardCardsDto> GetAppealDashboardCardsAsync(CancellationToken ct = default)
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var overdueCutoff = today.AddDays(-30);

        var raw = await _appealRepository.GetQueryable()
            .Where(a => a.IsActive && !a.MarkedForDeletion)
            .GroupBy(x => 1)
            .Select(g => new
            {
                TotalAppeals = g.Count(),
                PendingAppeals = g.Count(a => a.AppealStatus == "Pending" || a.AppealStatus == "Submitted"),
                ApprovedAppeals = g.Count(a => a.AppealStatus == "Approved" || a.AppealStatus == "Allowed"),
                RejectedAppeals = g.Count(a => a.AppealStatus == "Rejected" || a.AppealStatus == "Dismissed"),
                ReturnedAppeals = g.Count(a => a.AppealStatus == "Returned"),
                ResolvedAppeals = g.Count(a => a.AppealStatus == "Approved" || a.AppealStatus == "Allowed" ||
                                              a.AppealStatus == "Rejected" || a.AppealStatus == "Dismissed" ||
                                              a.AppealStatus == "Returned"),
                FirstAppeals = g.Count(a => a.AppealLevel == 1),
                SecondAppeals = g.Count(a => a.AppealLevel == 2),
                DelayedAppeals = g.Count(a => a.ReasonForComplaint != null && a.ReasonForComplaint.Contains("विलंब माफीचे कारण")),
                OverdueAppeals = g.Count(a => (a.AppealStatus == "Pending" || a.AppealStatus == "Submitted") &&
                                              a.CreatedDate.HasValue && a.CreatedDate.Value < overdueCutoff),
                TodayAppeals = g.Count(a => a.CreatedDate.HasValue &&
                                            a.CreatedDate.Value >= today &&
                                            a.CreatedDate.Value < tomorrow)
            })
            .FirstOrDefaultAsync(ct);

        int total = raw?.TotalAppeals ?? 0;
        int pending = raw?.PendingAppeals ?? 0;
        int approved = raw?.ApprovedAppeals ?? 0;
        int rejected = raw?.RejectedAppeals ?? 0;
        int returned = raw?.ReturnedAppeals ?? 0;
        int todayCount = raw?.TodayAppeals ?? 0;
        int delayed = raw?.DelayedAppeals ?? 0;
        int overdue = raw?.OverdueAppeals ?? 0;

        return new RTSAppealDashboardCardsDto
        {
            TotalApplications = total,
            TotalAppeals = total,
            Pending = pending,
            PendingAppeals = pending,
            Approved = approved,
            ApprovedAppeals = approved,
            Rejected = rejected,
            RejectedAppeals = rejected,
            Returned = returned,
            ReturnedAppeals = returned,
            FirstAppeals = raw?.FirstAppeals ?? 0,
            SecondAppeals = raw?.SecondAppeals ?? 0,
            DelayedAppeals = delayed,
            OverdueAppeals = overdue,
            OverdueApplications = overdue,
            TodayApplications = todayCount,
            TodayAppeals = todayCount,
            DueToday = overdue,
            ResolvedAppeals = raw?.ResolvedAppeals ?? 0,

            PendingPercentage = total > 0 ? Math.Round((decimal)pending / total * 100, 2) : 0,
            ApprovedPercentage = total > 0 ? Math.Round((decimal)approved / total * 100, 2) : 0,
            RejectedPercentage = total > 0 ? Math.Round((decimal)rejected / total * 100, 2) : 0,
            ReturnedPercentage = total > 0 ? Math.Round((decimal)returned / total * 100, 2) : 0,
            TodayPercentage = total > 0 ? Math.Round((decimal)todayCount / total * 100, 2) : 0,
            DelayedPercentage = total > 0 ? Math.Round((decimal)delayed / total * 100, 2) : 0,
            OverduePercentage = total > 0 ? Math.Round((decimal)overdue / total * 100, 2) : 0,
            DueTodayPercentage = total > 0 ? Math.Round((decimal)overdue / total * 100, 2) : 0
        };
    }

    /// <inheritdoc />
    public async Task<PagedResult<RTSAppealGridItemDto>> GetAppealDashboardGridAsync(
        RTSAppealQueryParameters queryParams,
        CancellationToken ct = default)
    {
        var query = _appealRepository.GetQueryable()
            .Where(a => a.IsActive && !a.MarkedForDeletion);

        // Status filter
        if (!string.IsNullOrWhiteSpace(queryParams.AppealStatus) &&
            !string.Equals(queryParams.AppealStatus, "All", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(queryParams.AppealStatus, "Overdue", StringComparison.OrdinalIgnoreCase))
            {
                var overdueCutoff = DateTime.Today.AddDays(-30);
                query = query.Where(a => (a.AppealStatus == "Pending" || a.AppealStatus == "Submitted") &&
                                         a.CreatedDate.HasValue && a.CreatedDate.Value < overdueCutoff);
            }
            else if (string.Equals(queryParams.AppealStatus, "Today", StringComparison.OrdinalIgnoreCase))
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);
                query = query.Where(a => a.CreatedDate.HasValue &&
                                         a.CreatedDate.Value >= today &&
                                         a.CreatedDate.Value < tomorrow);
            }
            else if (string.Equals(queryParams.AppealStatus, "Delayed", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(a => a.ReasonForComplaint != null && a.ReasonForComplaint.Contains("विलंब माफीचे कारण"));
            }
            else
            {
                query = query.Where(a => a.AppealStatus == queryParams.AppealStatus);
            }
        }

        // Level filter
        if (queryParams.AppealLevel.HasValue && queryParams.AppealLevel > 0)
        {
            query = query.Where(a => a.AppealLevel == queryParams.AppealLevel.Value);
        }

        // Appeal type filter
        if (queryParams.AppealTypeId.HasValue && queryParams.AppealTypeId > 0)
        {
            query = query.Where(a => a.AppealTypeId == queryParams.AppealTypeId.Value);
        }

        // Search term
        if (!string.IsNullOrWhiteSpace(queryParams.SearchTerm))
        {
            string term = queryParams.SearchTerm.Trim().ToLower();
            query = query.Where(a =>
                (a.AppealNo != null && a.AppealNo.ToLower().Contains(term)) ||
                (a.MobileNumber != null && a.MobileNumber.Contains(term)) ||
                (a.ReasonForComplaint != null && a.ReasonForComplaint.ToLower().Contains(term)));
        }

        int totalCount = await query.CountAsync(ct);

        // Sorting
        bool isAscending = string.Equals(queryParams.SortOrder, "asc", StringComparison.OrdinalIgnoreCase);
        query = (queryParams.SortBy?.ToLower()) switch
        {
            "appealno" => isAscending ? query.OrderBy(a => a.AppealNo) : query.OrderByDescending(a => a.AppealNo),
            "appeallevel" => isAscending ? query.OrderBy(a => a.AppealLevel) : query.OrderByDescending(a => a.AppealLevel),
            "appealstatus" => isAscending ? query.OrderBy(a => a.AppealStatus) : query.OrderByDescending(a => a.AppealStatus),
            "filingdate" or "createddate" => isAscending ? query.OrderBy(a => a.CreatedDate) : query.OrderByDescending(a => a.CreatedDate),
            _ => query.OrderByDescending(a => a.CreatedDate)
        };

        // Paging
        int pageNumber = queryParams.PageNumber > 0 ? queryParams.PageNumber : 1;
        int pageSize = queryParams.PageSize > 0 ? queryParams.PageSize : 10;

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        if (!items.Any())
        {
            return new PagedResult<RTSAppealGridItemDto>
            {
                Items = new List<RTSAppealGridItemDto>(),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        // Batch load applications
        var appIds = items.Select(a => a.ApplicationId).Distinct().ToList();
        var applications = await _applicationRepository.GetQueryable()
            .Include(a => a.Service)
                .ThenInclude(s => s.Department)
            .Include(a => a.Department)
            .Where(a => appIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, ct);

        // Batch load appeal types
        var typeIds = items.Select(a => a.AppealTypeId).Distinct().ToList();
        var types = await _appealTypeRepository.GetQueryable()
            .Where(t => typeIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.AppealTypeName, ct);

        // Batch load stages
        var allStages = await _appealStageRepository.GetQueryable().ToListAsync(ct);

        var resultList = items.Select(a =>
        {
            applications.TryGetValue(a.ApplicationId, out var app);
            types.TryGetValue(a.AppealTypeId, out var typeName);

            var appealStage = allStages
                .FirstOrDefault(s => app != null && s.AppealFlowId == app.ApprovalFlowId && s.StageOrder == a.AppealLevel)
                ?? allStages.FirstOrDefault(s => s.StageOrder == a.AppealLevel);

            bool isDelayed = a.ReasonForComplaint != null && a.ReasonForComplaint.Contains("विलंब माफीचे कारण");
            string appNo = app?.ApplicationNo ?? $"APP-{a.ApplicationId}";
            string applicantName = app?.ApplicantName ?? "";
            var serviceName = !string.IsNullOrWhiteSpace(app?.Service?.ServiceName)
                ? app.Service.ServiceName
                : (!string.IsNullOrWhiteSpace(app?.Service?.ServiceNameLocal) ? app.Service.ServiceNameLocal : "RTS Service");
            var departmentName = !string.IsNullOrWhiteSpace(app?.Department?.DepartmentName)
                ? app.Department.DepartmentName
                : (!string.IsNullOrWhiteSpace(app?.Service?.Department?.DepartmentName) ? app.Service.Department.DepartmentName : "Property Tax Department");

            return new RTSAppealGridItemDto
            {
                AppealId = a.Id,
                AppealNo = a.AppealNo,
                AppealLevel = a.AppealLevel,
                AppealLevelLabel = a.AppealLevel == 2 ? "2nd Appeal" : "1st Appeal",
                ApplicationId = a.ApplicationId,
                ApplicationNo = appNo,
                ServiceId = app?.ServiceId ?? 0,
                ServiceName = serviceName,
                DepartmentId = app?.DepartmentId ?? app?.Service?.DepartmentId ?? 0,
                DepartmentName = departmentName,
                ApplicantName = applicantName,
                ApplicantMobile = a.MobileNumber ?? app?.ApplicantMobileNo ?? "",
                ApplicantEmail = a.EmailAddress ?? "",
                AppealTypeId = a.AppealTypeId,
                AppealTypeName = typeName ?? "Statutory RTS Appeal",
                ReasonForComplaint = a.ReasonForComplaint ?? "",
                AppealStatus = a.AppealStatus ?? "Pending",
                FilingDate = a.CreatedDate,
                FilingCategory = isDelayed ? "Delayed" : "Normal",
                CurrentStageOrder = appealStage?.StageOrder ?? a.AppealLevel,
                CurrentStageName = appealStage?.StageName ?? (a.AppealLevel == 2 ? "Second Appellate Authority" : "First Appellate Authority"),
                CanApprove = appealStage?.CanApprove ?? true,
                CanReject = appealStage?.CanReject ?? true,
                CanReturn = appealStage?.CanReturn ?? true
            };
        }).ToList();

        return new PagedResult<RTSAppealGridItemDto>
        {
            Items = resultList,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    /// <inheritdoc />
    public async Task<RTSAppealProcessDetailsDto?> GetAppealProcessDetailsAsync(int appealId, CancellationToken ct = default)
    {
        var appeal = await _appealRepository.GetQueryable()
            .FirstOrDefaultAsync(a => a.Id == appealId && a.IsActive && !a.MarkedForDeletion, ct);

        if (appeal == null) return null;

        var app = await _applicationRepository.GetQueryable()
            .Include(a => a.Service)
                .ThenInclude(s => s.Department)
            .Include(a => a.Department)
            .Include(a => a.FieldValueData)
                .ThenInclude(f => f.FieldDefinition)
            .Include(a => a.TrackApplicationHistory)
            .FirstOrDefaultAsync(a => a.Id == appeal.ApplicationId, ct);

        var appealType = await _appealTypeRepository.GetQueryable()
            .FirstOrDefaultAsync(t => t.Id == appeal.AppealTypeId, ct);

        // Resolve appellate stage
        int approvalFlowId = app?.ApprovalFlowId ?? 0;
        RTSAppealFlowStageMasterEntity? appealStage = null;
        if (approvalFlowId > 0)
        {
            appealStage = await _appealStageRepository.GetQueryable()
                .FirstOrDefaultAsync(s => s.AppealFlowId == approvalFlowId && s.StageOrder == appeal.AppealLevel, ct);
        }
        if (appealStage == null)
        {
            appealStage = await _appealStageRepository.GetQueryable()
                .FirstOrDefaultAsync(s => s.StageOrder == appeal.AppealLevel, ct);
        }

        // Timeline history
        var appealHistories = await _appealHistoryRepository.GetQueryable()
            .Where(h => h.AppealId == appeal.Id && h.IsActive)
            .OrderBy(h => h.CreatedDate)
            .ToListAsync(ct);

        List<RTSAppealHistoryItemDto> timeline;
        if (appealHistories.Any())
        {
            timeline = appealHistories.Select(h => new RTSAppealHistoryItemDto
            {
                Id = h.Id,
                StageName = h.Action,
                Action = h.Action,
                ActionByName = $"Officer #{h.ActionByUserId}",
                Status = h.Status,
                Remark = h.Remark,
                ActionDate = h.CreatedDate
            }).ToList();
        }
        else
        {
            timeline = app?.TrackApplicationHistory?
                .OrderBy(h => h.CreatedDate)
                .Select(h => new RTSAppealHistoryItemDto
                {
                    Id = h.Id,
                    StageName = null,
                    Action = h.Action,
                    ActionByName = $"User #{h.ActionByUserId}",
                    Status = h.Status,
                    Remark = h.Remark,
                    ActionDate = h.CreatedDate
                }).ToList() ?? new List<RTSAppealHistoryItemDto>();
        }

        var rejectHistory = app?.TrackApplicationHistory?
            .Where(h => string.Equals(h.Status, "Rejected", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(h.Action, "Rejected", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(h => h.CreatedDate)
            .FirstOrDefault();

        string serviceName = !string.IsNullOrWhiteSpace(app?.Service?.ServiceName)
            ? app.Service.ServiceName
            : (!string.IsNullOrWhiteSpace(app?.Service?.ServiceNameLocal) ? app.Service.ServiceNameLocal : "RTS Service");

        string departmentName = !string.IsNullOrWhiteSpace(app?.Department?.DepartmentName)
            ? app.Department.DepartmentName
            : (!string.IsNullOrWhiteSpace(app?.Service?.Department?.DepartmentName) ? app.Service.Department.DepartmentName : "Property Tax Department");

        var dynamicFields = app?.FieldValueData?
            .Where(f => f.FieldDefinition != null && f.FieldDefinition.FieldGroup != "Document Uploads")
            .OrderBy(f => f.FieldDefinition?.DisplayOrder ?? 0)
            .Select(f => new RTSAppealFieldAnswerDto
            {
                FieldId = f.FieldDefinitionId,
                FieldName = f.FieldDefinition?.FieldCode ?? "",
                FieldLabel = !string.IsNullOrWhiteSpace(f.FieldDefinition?.FieldLabel)
                    ? f.FieldDefinition.FieldLabel
                    : (!string.IsNullOrWhiteSpace(f.FieldDefinition?.FieldLabelLocal) ? f.FieldDefinition.FieldLabelLocal : f.FieldDefinition?.FieldCode ?? ""),
                FieldValue = f.TextValue ?? f.NumberValue?.ToString() ?? f.DateValue?.ToString("yyyy-MM-dd") ?? (f.BooleanValue.HasValue ? f.BooleanValue.Value.ToString() : null),
                DisplayValue = f.TextValue ?? f.NumberValue?.ToString() ?? f.DateValue?.ToString("yyyy-MM-dd") ?? (f.BooleanValue.HasValue ? f.BooleanValue.Value.ToString() : null)
            }).ToList() ?? new List<RTSAppealFieldAnswerDto>();

        var documents = app?.FieldValueData?
            .Where(f => f.FieldDefinition != null && f.FieldDefinition.FieldGroup == "Document Uploads")
            .Select(f => new RTSAppealDocumentDto
            {
                DocumentId = f.FieldDefinitionId,
                DocumentName = f.FieldDefinition?.FieldLabel ?? "Document",
                DocumentType = f.FieldDefinition?.FieldType ?? "",
                DocumentPath = f.DocumentGuid.HasValue ? f.DocumentGuid.ToString()! : "",
                IsMandatory = f.FieldDefinition?.IsRequired ?? false
            }).ToList() ?? new List<RTSAppealDocumentDto>();

        bool isFinal = string.Equals(appeal.AppealStatus, "Approved", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(appeal.AppealStatus, "Allowed", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(appeal.AppealStatus, "Rejected", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(appeal.AppealStatus, "Dismissed", StringComparison.OrdinalIgnoreCase);

        return new RTSAppealProcessDetailsDto
        {
            AppealId = appeal.Id,
            AppealNo = appeal.AppealNo,
            AppealLevel = appeal.AppealLevel,
            AppealLevelLabel = appeal.AppealLevel == 2 ? "2nd Appeal" : "1st Appeal",
            AppealStatus = appeal.AppealStatus ?? "Pending",
            FilingDate = appeal.CreatedDate,
            AppealTypeId = appeal.AppealTypeId,
            AppealTypeName = appealType?.AppealTypeName ?? "Statutory RTS Appeal",
            ReasonForComplaint = appeal.ReasonForComplaint ?? "",
            ActionRemarks = appeal.ActionRemarks,
            ApplicationId = appeal.ApplicationId,
            ApplicationNo = app?.ApplicationNo ?? "",
            ServiceId = app?.ServiceId ?? 0,
            ServiceName = serviceName,
            DepartmentId = app?.DepartmentId ?? app?.Service?.DepartmentId ?? 0,
            DepartmentName = departmentName,
            ApplicantName = app?.ApplicantName ?? "",
            ApplicantMobile = appeal.MobileNumber ?? app?.ApplicantMobileNo ?? "",
            ApplicantEmail = appeal.EmailAddress ?? "",
            OriginalApplicationStatus = app?.ApplicationStatus ?? "Pending",
            OriginalApplicationDate = app?.CreatedDate,
            OriginalRejectionDate = rejectHistory?.CreatedDate,
            OriginalRejectionReason = rejectHistory?.Remark,
            DynamicFields = dynamicFields,
            Documents = documents,
            Timeline = timeline,
            CanApprove = !isFinal && (appealStage?.CanApprove ?? true),
            CanReject = !isFinal && (appealStage?.CanReject ?? true),
            CanReturn = !isFinal && (appealStage?.CanReturn ?? true),
            CurrentStageName = appealStage?.StageName ?? (appeal.AppealLevel == 2 ? "Second Appellate Desk" : "First Appellate Desk")
        };
    }

    /// <inheritdoc />
    public async Task<bool> ProcessAppealOfficerActionAsync(ProcessRTSAppealActionDto dto, CancellationToken ct = default)
    {
        if (dto.AppealId <= 0 || string.IsNullOrWhiteSpace(dto.Action))
        {
            throw new ArgumentException("Invalid appeal action request.");
        }

        if (string.IsNullOrWhiteSpace(dto.Remark))
        {
            throw new ArgumentException("Remark / official order is mandatory for processing an appeal action.");
        }

        var appeal = await _appealRepository.GetQueryable()
            .FirstOrDefaultAsync(a => a.Id == dto.AppealId && a.IsActive && !a.MarkedForDeletion, ct);

        if (appeal == null)
        {
            throw new KeyNotFoundException($"Appeal with ID {dto.AppealId} was not found.");
        }

        if (!string.Equals(appeal.AppealStatus, "Pending", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(appeal.AppealStatus, "Submitted", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Appeal #{appeal.AppealNo} has already been actioned ({appeal.AppealStatus}). No further actions can be taken on this appeal.");
        }

        var app = await _applicationRepository.GetQueryable()
            .FirstOrDefaultAsync(a => a.Id == appeal.ApplicationId && a.IsActive && !a.MarkedForDeletion, ct);

        int approvalFlowId = app?.ApprovalFlowId ?? 0;
        RTSAppealFlowStageMasterEntity? appealStage = null;
        if (approvalFlowId > 0)
        {
            appealStage = await _appealStageRepository.GetQueryable()
                .FirstOrDefaultAsync(s => s.AppealFlowId == approvalFlowId && s.StageOrder == appeal.AppealLevel, ct);
        }
        if (appealStage == null)
        {
            appealStage = await _appealStageRepository.GetQueryable()
                .FirstOrDefaultAsync(s => s.StageOrder == appeal.AppealLevel, ct);
        }

        // Authorization check: if stage has designated UserId and current user is officer, enforce role check
        int currentUserId = 0;
        try
        {
            currentUserId = _currentUserService.GetCurrentUserId();
        }
        catch
        {
            currentUserId = 0;
        }

        if (appealStage != null && appealStage.UserId > 0 && currentUserId > 0 && currentUserId != appealStage.UserId)
        {
            _logger?.LogWarning("User {UserId} is not authorized for Appellate Stage {Stage} (Assigned to {AssignedUser})",
                currentUserId, appealStage.StageName, appealStage.UserId);
        }

        string normalizedAction = dto.Action.Trim();
        string newAppealStatus;
        string timelineAction;

        if (string.Equals(normalizedAction, "Approve", StringComparison.OrdinalIgnoreCase))
        {
            newAppealStatus = "Approved";
            timelineAction = $"{appeal.AppealLevel}{(appeal.AppealLevel == 2 ? "nd" : "st")} Appeal Allowed / Approved";
        }
        else if (string.Equals(normalizedAction, "Reject", StringComparison.OrdinalIgnoreCase))
        {
            newAppealStatus = "Rejected";
            timelineAction = $"{appeal.AppealLevel}{(appeal.AppealLevel == 2 ? "nd" : "st")} Appeal Dismissed / Rejected";
        }
        else if (string.Equals(normalizedAction, "Return", StringComparison.OrdinalIgnoreCase))
        {
            newAppealStatus = "Returned";
            timelineAction = $"{appeal.AppealLevel}{(appeal.AppealLevel == 2 ? "nd" : "st")} Appeal Remanded / Returned with Directions";
        }
        else
        {
            throw new ArgumentException($"Invalid action '{dto.Action}'. Supported actions are 'Approve', 'Reject', and 'Return'.");
        }

        int officerUserId = currentUserId > 0 ? currentUserId : (appealStage?.UserId ?? 1);
        DateTime now = DateTime.Now;

        // Transactional update: appeal status + track appeal history
        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            appeal.AppealStatus = newAppealStatus;
            appeal.ActionRemarks = dto.Remark;
            appeal.ActionByUserId = officerUserId;
            appeal.ActionDate = now;
            appeal.UpdatedDate = now;
            appeal.UpdatedBy = officerUserId;

            await _appealRepository.UpdateAsync(appeal, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            var historyEntry = new RTSTrackAppealHistoryEntity
            {
                AppealId = appeal.Id,
                ApplicationId = appeal.ApplicationId,
                AppealLevel = appeal.AppealLevel,
                ApprovalFlowId = approvalFlowId > 0 ? approvalFlowId : null,
                AppealFlowStageId = appealStage?.Id,
                ActionByUserId = officerUserId,
                Status = newAppealStatus,
                Action = timelineAction,
                Remark = dto.Remark,
                IsActive = true,
                CreatedDate = now
            };

            await _appealHistoryRepository.AddAsync(historyEntry, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            await _unitOfWork.CommitTransactionAsync(ct);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }

        _logger?.LogInformation("RTS Appeal {AppealNo} processed successfully: Status={Status}, Officer={OfficerId}",
            appeal.AppealNo, newAppealStatus, officerUserId);

        return true;
    }

    /// <inheritdoc />
    public async Task<RTSAppealDashboardCardsCountDto> GetDashboardCardsDataAsync(CancellationToken ct = default)
        => await GetAppealDashboardCardsAsync(ct);

    /// <inheritdoc />
    public async Task<PagedResult<RTSAppealDashboardDetailsDto>> GetAllDashboardAppealsAsync(
        RTSAppealQueryParameters queryParams,
        CancellationToken ct = default)
    {
        var grid = await GetAppealDashboardGridAsync(queryParams, ct);
        return new PagedResult<RTSAppealDashboardDetailsDto>(
            grid.Items.Cast<RTSAppealDashboardDetailsDto>().ToList(),
            grid.TotalCount,
            grid.PageNumber,
            grid.PageSize);
    }

    /// <inheritdoc />
    public Task<RTSAppealProcessDetailsDto?> ViewAppealSummaryAsync(int appealId, CancellationToken ct = default)
        => GetAppealProcessDetailsAsync(appealId, ct);

    /// <inheritdoc />
    public Task<bool> ProcessAppealActionAsync(ProcessRTSAppealActionDto dto, CancellationToken ct = default)
        => ProcessAppealOfficerActionAsync(dto, ct);
}

/// <summary>
/// Backward-compatibility alias for previous casing.
/// </summary>
public class RtsAppealService : RTSAppealService
{
    public RtsAppealService(
        IRepository<RTSAppealApplicationEntity, int> appealRepository,
        IRepository<RTSAppealTypeMasterEntity, int> appealTypeRepository,
        IRepository<RTSAppealFlowStageMasterEntity, int> appealStageRepository,
        IRepository<RTSTrackAppealHistoryEntity, int> appealHistoryRepository,
        IRepository<RTSApplicationDetailsEntity, int> applicationRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<RTSAppealService>? logger = null)
        : base(appealRepository, appealTypeRepository, appealStageRepository, appealHistoryRepository, applicationRepository, currentUserService, unitOfWork, logger)
    {
    }
}
