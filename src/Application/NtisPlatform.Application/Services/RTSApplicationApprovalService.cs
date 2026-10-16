using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.Constants;
using NtisPlatform.Application.DTOs.Document;
using NtisPlatform.Application.DTOs.RTSApplication;
using NtisPlatform.Application.DTOs.RTSApplicationApproval;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class RTSApplicationApprovalService : BaseCommonCrudService<RTSApplicationDetailsEntity, RTSApplicationDetailsDto, CreateRTSApplicationDetailsDto, UpdateRTSApplicationDetailsDto, RTSApplicationQueryParameters, int>, IRTSApplicationApprovalService
{
    private readonly IRepository<RTSApprovalFlowStageMasterEntity, int> _approvalFlowStageRepository;
    private readonly IRepository<UserEntity, int> _userRepository;
    private readonly IRepository<TrackApplicationHistoryEntity, int> _historyRepository;
    private readonly IRepository<RTSFieldValueEntity, int> _fieldValueRepository;
    private readonly IRepository<RTSPaymentDemandEntity, long> _demandRepository;
    private readonly IRepository<RTSPaymentTransactionEntity, long> _paymentRepository;
    private readonly IRepository<RTSServiceEntity, int> _serviceRepository;
    private readonly IRepository<RTSIssuedCertificateEntity, int> _issuedCertificateRepository;
    private readonly IRTSSmsNotificationService _smsNotificationService;
    private readonly IAapleSarkarIntegrationService _aapleSarkarService;
    private readonly IRTSStageAllocationRuleService _allocationRules;
    private readonly IDocumentApplicationService _documentApplicationService;
    private readonly IRepository<DocumentEntity, int> _documentRepository;
    private readonly IRepository<DocumentBindingEntity, int> _bindingRepository;

    public RTSApplicationApprovalService(
          IRepository<RTSApplicationDetailsEntity, int> repository,
          IRepository<RTSApprovalFlowStageMasterEntity, int> approvalFlowStageRepository,
          IRepository<TrackApplicationHistoryEntity, int> historyRepository,
          IRepository<UserEntity, int> userRepository,
          IRepository<RTSFieldValueEntity, int> fieldValueRepository,
          IRepository<RTSPaymentDemandEntity, long> demandRepository,
          IRepository<RTSPaymentTransactionEntity, long> paymentRepository,
          IRepository<RTSServiceEntity, int> serviceRepository,
          IRepository<RTSIssuedCertificateEntity, int> issuedCertificateRepository,
          IRTSSmsNotificationService smsNotificationService,
          IAapleSarkarIntegrationService aapleSarkarService,
          IRTSStageAllocationRuleService allocationRules,
          IDocumentApplicationService documentApplicationService,
          IRepository<DocumentEntity, int> documentRepository,
          IRepository<DocumentBindingEntity, int> bindingRepository,
          IUnitOfWork unitOfWork,
          IMapper mapper) : base(repository, unitOfWork, mapper)
    {
        _approvalFlowStageRepository = approvalFlowStageRepository;
        _userRepository = userRepository;
        _historyRepository = historyRepository;
        _fieldValueRepository = fieldValueRepository;
        _demandRepository = demandRepository;
        _paymentRepository = paymentRepository;
        _serviceRepository = serviceRepository;
        _smsNotificationService = smsNotificationService;
        _issuedCertificateRepository = issuedCertificateRepository;
        _aapleSarkarService = aapleSarkarService;
        _allocationRules = allocationRules;
        _documentApplicationService = documentApplicationService;
        _documentRepository = documentRepository;
        _bindingRepository = bindingRepository;
    }

    private async Task<int> ResolveStageOfficerAsync(RTSApplicationDetailsEntity application,
        int stageId, CancellationToken cancellationToken)
    {
        var values = await _fieldValueRepository.GetQueryable().AsNoTracking()
            .Where(x => x.ApplicationId == application.Id && x.IsActive && !x.MarkedForDeletion)
            .ToListAsync(cancellationToken);
        return await _allocationRules.ResolveOfficerAsync(application.ServiceId, application.ApprovalFlowId,
            stageId, _allocationRules.NormalizeValues(values), cancellationToken);
    }

    private async Task EnsurePaymentSnapshotIsSettledAsync(
        int applicationId,
        string? applicationNo,
        string action,
        CancellationToken cancellationToken)
    {
        var demand = await _demandRepository.GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.ApplicationId == applicationId && d.IsActive, cancellationToken);

        if (demand != null && demand.TotalAmount > 0)
        {
            if (string.Equals(demand.DemandStatus, "Paid", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var paymentSnapshot = await _paymentRepository.GetQueryable()
                .AsNoTracking()
                .Include(payment => payment.PaymentStatus)
                .Where(payment => payment.ApplicationId == applicationId && payment.IsActive && !payment.IsReceiptCancelled)
                .OrderByDescending(payment => payment.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (paymentSnapshot != null &&
                string.Equals(paymentSnapshot.PaymentStatus?.StatusCode, "SUCCESS", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            throw new InvalidOperationException(
                $"Cannot {action} application {applicationNo}. Statutory fee of ₹{demand.TotalAmount:F2} is pending under Demand {demand.DemandNo}. Payment must be recorded before proceeding.");
        }

        var legacyPayment = await _paymentRepository.GetQueryable()
            .AsNoTracking()
            .Include(payment => payment.PaymentStatus)
            .Where(payment => payment.ApplicationId == applicationId && payment.IsActive && !payment.IsReceiptCancelled)
            .OrderByDescending(payment => payment.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (legacyPayment == null || legacyPayment.TotalAmount <= 0 ||
            string.Equals(legacyPayment.PaymentStatus?.StatusCode, "SUCCESS", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Cannot {action} application {applicationNo}. Statutory fee of ₹{legacyPayment.TotalAmount:F2} is pending. Payment must be recorded before proceeding.");
    }

    public async Task<RTSApplicationDashboardCardsCountDto> GetDashboardCardsDataAsync(CancellationToken cancellationToken = default)
    {
        var query = _repository.GetQueryable().AsNoTracking().Where(x => !x.MarkedForDeletion);
        var dashboard = await query
            .GroupBy(x => 1)
            .Select(g => new
            {
                TotalApplications = g.Count(),
                Pending = g.Count(x => x.ApplicationStatus != ApplicationStatus.Approved &&
                    x.ApplicationStatus != ApplicationStatus.Rejected &&x.ApplicationStatus!=ApplicationStatus.Reverted) ,
                Approved = g.Count(x => x.ApplicationStatus == ApplicationStatus.Approved),
                Rejected = g.Count(x => x.ApplicationStatus == ApplicationStatus.Rejected),
                Reverted = g.Count(x => x.IsReverted),
                PendingPercentage = 0,
                TodayApplications = g.Count(x => x.CreatedDate.HasValue && x.CreatedDate.Value.Date == DateTime.Today),
                OverdueApplications = g.Count(x =>
                    x.ApplicationStatus != ApplicationStatus.Approved &&
                    x.ApplicationStatus != ApplicationStatus.Rejected &&
                    x.Service.Sla != null &&
                    x.Service.Sla.Contains(" ") &&
                    x.CreatedDate.HasValue &&
                    x.CreatedDate.Value.AddDays(Convert.ToInt32(x.Service.Sla.Substring(0, x.Service.Sla.IndexOf(" ")))) < DateTime.Today),
                DueToday = g.Count(x =>
                    x.ApplicationStatus != ApplicationStatus.Approved &&
                    x.ApplicationStatus != ApplicationStatus.Rejected &&
                    x.Service.Sla != null &&
                    x.Service.Sla.Contains(" ") &&
                    x.CreatedDate.HasValue &&
                    x.CreatedDate.Value.AddDays(Convert.ToInt32(x.Service.Sla.Substring(0, x.Service.Sla.IndexOf(" ")))).Date == DateTime.Today)
            }).SingleOrDefaultAsync(cancellationToken);


        return new RTSApplicationDashboardCardsCountDto
        {
            TotalApplications = dashboard?.TotalApplications ?? 0,
            Pending = dashboard?.Pending ?? 0,
            Approved = dashboard?.Approved ?? 0,
            Rejected = dashboard?.Rejected ?? 0,
            Reverted = dashboard?.Reverted ?? 0,
            TodayApplications = dashboard?.TodayApplications ?? 0,
            OverdueApplications = dashboard?.OverdueApplications ?? 0,
            DueToday = dashboard?.DueToday ?? 0,
            PendingPercentage = dashboard?.TotalApplications > 0 ? Math.Round((decimal)(dashboard?.Pending ?? 0) / (dashboard?.TotalApplications ?? 1) * 100, 2) : 0,
            ApprovedPercentage = dashboard?.TotalApplications > 0 ? Math.Round((decimal)(dashboard?.Approved ?? 0) / (dashboard?.TotalApplications ?? 1) * 100, 2) : 0,
            RejectedPercentage = dashboard?.TotalApplications > 0 ? Math.Round((decimal)(dashboard?.Rejected ?? 0) / (dashboard?.TotalApplications ?? 1) * 100, 2) : 0,
            RevertedPercentage = dashboard?.TotalApplications > 0 ? Math.Round((decimal)(dashboard?.Reverted ?? 0) / (dashboard?.TotalApplications ?? 1) * 100, 2) : 0,
            TodayPercentage = dashboard?.TotalApplications > 0 ? Math.Round((decimal)(dashboard?.TodayApplications ?? 0) / (dashboard?.TotalApplications ?? 1) * 100, 2) : 0,
            OverduePercentage = dashboard?.TotalApplications > 0 ? Math.Round((decimal)(dashboard?.OverdueApplications ?? 0) / (dashboard?.TotalApplications ?? 1) * 100, 2) : 0,
            DueTodayPercentage = dashboard?.TotalApplications > 0 ? Math.Round((decimal)(dashboard?.DueToday ?? 0) / (dashboard?.TotalApplications ?? 1) * 100, 2) : 0
        };
    }


    public async Task<PagedResult<RTSApplicationDashboardDetailsDto>> GetAllDashboardApplicationAsync(
    RTSApplicationQueryParameters queryParameters,
    CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;

        var query = _repository.GetQueryable()
         .Where(x => !x.MarkedForDeletion && x.IsActive)
         .AsQueryable();

        if (queryParameters.DepartmentId > 0)
            query = query.Where(x => x.DepartmentId == queryParameters.DepartmentId);

        if (queryParameters.ServiceId > 0)
            query = query.Where(x => x.ServiceId == queryParameters.ServiceId);

        if (queryParameters.UserId.HasValue && queryParameters.UserId.Value > 0)
            query = query.Where(x => x.UserId == queryParameters.UserId.Value);

        // ADD DATE FILTER HERE
        if (queryParameters.FromDate.HasValue)
        {
            var fromDate = queryParameters.FromDate.Value.Date;

            query = query.Where(x =>
                x.CreatedDate.HasValue &&
                x.CreatedDate.Value >= fromDate);
        }

        if (queryParameters.ToDate.HasValue)
        {
            var toDate = queryParameters.ToDate.Value.Date.AddDays(1);

            query = query.Where(x =>
                x.CreatedDate.HasValue &&
                x.CreatedDate.Value < toDate);
        }



        var search = (!string.IsNullOrWhiteSpace(queryParameters.SearchTerm)
            ? queryParameters.SearchTerm
            : queryParameters.ApplicationNo)?.Trim().ToLower();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                (x.ApplicationNo != null && x.ApplicationNo.ToLower().Contains(search)) ||
                (x.ApplicantName != null && x.ApplicantName.ToLower().Contains(search)) ||
                (x.ApplicantMobileNo != null && x.ApplicantMobileNo.ToLower().Contains(search)) ||
                (x.CitizenSession != null && x.CitizenSession.PropertyNo != null && x.CitizenSession.PropertyNo.ToLower().Contains(search)) ||
                (x.CitizenSession != null && x.CitizenSession.Upic != null && x.CitizenSession.Upic.ToLower().Contains(search)));
        }

        if (string.Equals(queryParameters.ApplicationStatus, ApplicationStatus.Pending.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x => x.ApplicationStatus != ApplicationStatus.Approved && x.ApplicationStatus != ApplicationStatus.Rejected && x.ApplicationStatus != ApplicationStatus.Reverted);
        }
        else if (string.Equals(queryParameters.ApplicationStatus, "Overdue Applications", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x =>
                x.ApplicationStatus != ApplicationStatus.Approved &&
                x.ApplicationStatus != ApplicationStatus.Rejected &&
                x.CreatedDate.HasValue &&
                !string.IsNullOrWhiteSpace(x.Service.Sla) &&
                x.Service.Sla.Contains(" ") &&
                x.CreatedDate.Value.AddDays(
                    Convert.ToInt32(
                        x.Service.Sla.Substring(0, x.Service.Sla.IndexOf(" ")).Trim()
                    )
                ) < today);
        }
        else if (string.Equals(queryParameters.ApplicationStatus, "DueToday", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x =>
                x.ApplicationStatus != ApplicationStatus.Approved &&
                x.ApplicationStatus != ApplicationStatus.Rejected &&
                x.Service != null &&
                x.Service.Sla != null &&
                x.Service.Sla.Contains(" ") &&
                x.CreatedDate.HasValue &&
                x.CreatedDate.Value.AddDays(
                    Convert.ToInt32(x.Service.Sla.Substring(0, x.Service.Sla.IndexOf(" ")))
                ).Date == today.Date);
        }
        else if (string.Equals(queryParameters.ApplicationStatus, "Today's Applications", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(queryParameters.ApplicationStatus, "Todays Applications", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x =>
                x.IsActive &&
                x.CreatedDate.HasValue &&
                x.CreatedDate.Value.Date == today.Date);
        }
        else if (!string.IsNullOrWhiteSpace(queryParameters.ApplicationStatus))
        {
            var targetStatus = queryParameters.ApplicationStatus.Trim().ToLower();
            query = query.Where(x => x.ApplicationStatus != null && x.ApplicationStatus.ToLower() == targetStatus);
        }

        // RemainingDays is a computed field (not on entity), so skip entity-level sort when sorting by it
        var isSortByRemainingDays = string.Equals(queryParameters.SortBy, "RemainingDays", StringComparison.OrdinalIgnoreCase);
        if (string.Equals(queryParameters.SortBy, "CreatedDate", StringComparison.OrdinalIgnoreCase))
        {
            query = string.Equals(queryParameters.SortOrder, "desc", StringComparison.OrdinalIgnoreCase)
                ? query.OrderByDescending(x => x.CreatedDate).ThenByDescending(x => x.Id)
                : query.OrderBy(x => x.CreatedDate).ThenBy(x => x.Id);
        }
        else if (string.Equals(queryParameters.SortBy, "applicationNo", StringComparison.OrdinalIgnoreCase))
        {
            query = string.Equals(queryParameters.SortOrder, "desc", StringComparison.OrdinalIgnoreCase)
                ? query.OrderByDescending(x => x.ApplicationNo).ThenByDescending(x => x.Id)
                : query.OrderBy(x => x.ApplicationNo).ThenBy(x => x.Id);
        }
        else if (string.Equals(queryParameters.SortBy, "ApplicantName", StringComparison.OrdinalIgnoreCase))
        {
            query = string.Equals(queryParameters.SortOrder, "desc", StringComparison.OrdinalIgnoreCase)
                ? query.OrderByDescending(x => x.ApplicantName).ThenByDescending(x => x.Id)
                : query.OrderBy(x => x.ApplicantName).ThenBy(x => x.Id);
        }
        else if (string.Equals(queryParameters.SortBy, "ApplicationStatus", StringComparison.OrdinalIgnoreCase))
        {
            query = string.Equals(queryParameters.SortOrder, "desc", StringComparison.OrdinalIgnoreCase)
                ? query.OrderByDescending(x => x.ApplicationStatus).ThenByDescending(x => x.Id)
                : query.OrderBy(x => x.ApplicationStatus).ThenBy(x => x.Id);
        }
        else if (string.Equals(queryParameters.SortBy, "UpdatedDate", StringComparison.OrdinalIgnoreCase))
        {
            query = string.Equals(queryParameters.SortOrder, "desc", StringComparison.OrdinalIgnoreCase)
                ? query.OrderByDescending(x => x.UpdatedDate ?? x.CreatedDate).ThenByDescending(x => x.Id)
                : query.OrderBy(x => x.UpdatedDate ?? x.CreatedDate).ThenBy(x => x.Id);
        }
        else if (queryParameters.IsFifo == true || string.IsNullOrWhiteSpace(queryParameters.SortBy) || string.Equals(queryParameters.SortBy, "FIFO", StringComparison.OrdinalIgnoreCase))
        {
            // Strict FIFO mode:
            // 1. Pending/active applications first, then closed (Approved, Rejected, Reverted)
            // 2. Pending applications assigned to the logged-in officer (CurrentUserId) on top
            // 3. Earliest CreatedDate first (FIFO)
            // 4. Id ASC
            var currentUserId = queryParameters.CurrentUserId;
            if (currentUserId.HasValue && currentUserId.Value > 0)
            {
                var targetUserId = currentUserId.Value;
                query = query
                    .OrderBy(x => (x.ApplicationStatus != ApplicationStatus.Approved && x.ApplicationStatus != ApplicationStatus.Rejected && x.ApplicationStatus != ApplicationStatus.Reverted) ? 0 : 1)
                    .ThenBy(x => x.UserId == targetUserId ? 0 : 1)
                    .ThenBy(x => x.CreatedDate)
                    .ThenBy(x => x.Id);
            }
            else
            {
                query = query
                    .OrderBy(x => (x.ApplicationStatus != ApplicationStatus.Approved && x.ApplicationStatus != ApplicationStatus.Rejected && x.ApplicationStatus != ApplicationStatus.Reverted) ? 0 : 1)
                    .ThenBy(x => x.CreatedDate)
                    .ThenBy(x => x.Id);
            }
        }
        else if (!isSortByRemainingDays)
        {
            query = query.ApplySort<RTSApplicationDetailsEntity, RTSApplicationQueryParameters>(queryParameters);
        }
        else
        {
            // Apply default Id ordering so EF doesn't warn about unordered Skip/Take
            query = query.OrderBy(x => x.Id);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize)
            .Select(x => new RTSApplicationDashboardDetailsDto
            {
                Id = x.Id,
                DepartmentId = x.DepartmentId,
                ServiceId = x.ServiceId,
                ApplicationNo = x.ApplicationNo,
                ApplicationStatus = x.ApplicationStatus,
                ApplicantName = x.ApplicantName,
                PropertyNo=x.CitizenSession.PropertyNo,
                UpicId=x.CitizenSession.Upic,
                SessionId = x.SessionId,
                OwnerId = x.OwnerId,
                DepartmentName = x.Department.DepartmentName,
                DepartmentNameLocal = x.Department.DepartmentNameLocal,
                ServiceName = x.Service.ServiceName,
                ServiceNameLocal = x.Service.ServiceNameLocal,
                Sla = x.Service.Sla,
                Remark = x.Remark,

                ApplicantMobileNo = x.ApplicantMobileNo,
                UserId = x.UserId,
                UserName = x.UserId != null ? x.User.UserName : null,
                OfficerFirstName = x.User.FirstName,
                OfficerLastName = x.User.LastName,
                CreatedDate = x.CreatedDate,
                UpdatedDate = x.UpdatedDate,
            }).ToListAsync(cancellationToken);

        foreach (var item in items)
        {
            if (!item.CreatedDate.HasValue)
            {
                item.RemainingDays = null;
                continue;
            }
            var slaDays = ExtractSlaDays(item.Sla);
            if (!slaDays.HasValue)
            {
                item.RemainingDays = null;
                continue;
            }
            var dueDate = item.CreatedDate.Value.Date.AddDays(slaDays.Value);
            var diff = (dueDate - DateTime.Today).Days;
            item.RemainingDays = diff;
        }

        // Apply in-memory sort for the computed RemainingDays field (nulls pushed to end)
        if (isSortByRemainingDays)
        {
            items = string.Equals(queryParameters.SortOrder, "desc", StringComparison.OrdinalIgnoreCase)
                ? items.OrderByDescending(x => x.RemainingDays.HasValue)
                       .ThenByDescending(x => x.RemainingDays)
                       .ToList()
                : items.OrderByDescending(x => x.RemainingDays.HasValue)
                       .ThenBy(x => x.RemainingDays)
                       .ToList();
        }


        var pageNumber = queryParameters.PageNumber;
        var pageSize = queryParameters.PageSize;

        if (queryParameters.PageSize == -1)
        {
            pageNumber = 1;
            pageSize = Math.Max(1, totalCount);
        }

        return new PagedResult<RTSApplicationDashboardDetailsDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }


    public async Task<ApplicationApprovalStageDetailsDto?> GetApplicationApprovalStagesAsync(int applicationId, CancellationToken cancellationToken = default)
    {
        if (applicationId <= 0)
        {
            throw new ArgumentException("Invalid Application ID", nameof(applicationId));
        }

        var application = await _repository.GetQueryable()
            .AsNoTracking()
            .Where(x => !x.MarkedForDeletion && x.Id == applicationId)
            .Select(x => new
            {
                x.Id,
                x.ServiceId,
                x.ApprovalFlowId,
                x.CurrentApprovalFlowStageId,
                x.CurrentStageOrder,
                x.UserId,
                x.ApplicationStatus,
                x.IsReverted
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (application == null)
            return null;

        var stages = await _approvalFlowStageRepository.GetQueryable()
            .AsNoTracking()
            .Where(stage => stage.ApprovalFlowId == application.ApprovalFlowId)
            .OrderBy(stage => stage.StageOrder)
            .Select(stage => new { stage.Id, stage.StageOrder, stage.StageName })
            .ToListAsync(cancellationToken);

        // Historical applications may predate the persisted ApprovalFlowId.  Use the current
        // active service workflow only to build their read-only timeline; never repair data here.
        if (stages.Count == 0)
        {
            var fallbackFlowId = await _approvalFlowStageRepository.GetQueryable()
                .AsNoTracking()
                .Where(stage => stage.ApprovalFlow.ServiceId == application.ServiceId)
                .Select(stage => stage.ApprovalFlowId)
                .Distinct()
                .OrderByDescending(flowId => flowId)
                .FirstOrDefaultAsync(cancellationToken);
            if (fallbackFlowId > 0)
            {
                stages = await _approvalFlowStageRepository.GetQueryable()
                    .AsNoTracking()
                    .Where(stage => stage.ApprovalFlowId == fallbackFlowId)
                    .OrderBy(stage => stage.StageOrder)
                    .Select(stage => new { stage.Id, stage.StageOrder, stage.StageName })
                    .ToListAsync(cancellationToken);
            }
        }

        var histories = await _historyRepository.GetQueryable()
            .AsNoTracking()
            .Where(history => history.ApplicationId == applicationId &&
                              history.IsActive && history.ApprovalFlowStageId.HasValue)
            .OrderByDescending(history => history.Id)
            .Select(history => new
            {
                StageId = history.ApprovalFlowStageId!.Value,
                history.ActionByUserId,
                history.Status,
                history.Remark,
                history.CreatedDate
            })
            .ToListAsync(cancellationToken);
        var latestHistoryByStage = histories
            .GroupBy(history => history.StageId)
            .ToDictionary(group => group.Key, group => group.First());

        var isTerminal = string.Equals(application.ApplicationStatus, ApplicationStatus.Approved, StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(application.ApplicationStatus, ApplicationStatus.Rejected, StringComparison.OrdinalIgnoreCase);
        var revertedToCitizen = application.IsReverted &&
                                string.Equals(application.ApplicationStatus, ApplicationStatus.Reverted, StringComparison.OrdinalIgnoreCase) &&
                                !stages.Any(stage => stage.StageOrder < application.CurrentStageOrder);

        // Future stages have not yet produced history and therefore do not have a
        // persisted application UserId. Resolve their configured allocation from
        // the saved application values for an informative timeline preview. This
        // does not write or alter routing; the same resolver runs when the stage
        // is actually reached.
        var applicationValues = await _fieldValueRepository.GetQueryable()
            .AsNoTracking()
            .Where(value => value.ApplicationId == applicationId &&
                            value.IsActive && !value.MarkedForDeletion)
            .ToListAsync(cancellationToken);
        var normalizedValues = _allocationRules.NormalizeValues(applicationValues);
        var futureStageUserIds = new Dictionary<int, int>();
        foreach (var stage in stages)
        {
            var isCurrentStage = !isTerminal && !revertedToCitizen &&
                                 stage.Id == application.CurrentApprovalFlowStageId;
            if (isCurrentStage ||
                (latestHistoryByStage.TryGetValue(stage.Id, out var history) &&
                 IsCompletedStatus(history.Status)))
                continue;

            try
            {
                futureStageUserIds[stage.Id] = await _allocationRules.ResolveOfficerAsync(
                    application.ServiceId,
                    application.ApprovalFlowId,
                    stage.Id,
                    normalizedValues,
                    cancellationToken);
            }
            catch (InvalidOperationException)
            {
                // A stage without a resolvable allocation remains unassigned in
                // the preview. Never substitute RTSApprovalFlowStageMaster.UserId.
            }
        }

        var userIds = histories.Where(history => history.ActionByUserId.HasValue)
            .Select(history => history.ActionByUserId!.Value)
            .Append(application.UserId ?? 0)
            .Concat(futureStageUserIds.Values)
            .Where(userId => userId > 0)
            .Distinct()
            .ToList();
        var users = await _userRepository.GetQueryable()
            .AsNoTracking()
            .Where(user => userIds.Contains(user.Id) && !user.MarkedForDeletion)
            .Select(user => new { user.Id, user.UserName, user.FirstName, user.LastName })
            .ToDictionaryAsync(user => user.Id, cancellationToken);

        var result = new ApplicationApprovalStageDetailsDto
        {
            isRevertedToCitizen = revertedToCitizen,
            ApprovalStages = stages.Select(stage =>
            {
                latestHistoryByStage.TryGetValue(stage.Id, out var history);
                var isCurrentStage = !isTerminal && !revertedToCitizen &&
                                     stage.Id == application.CurrentApprovalFlowStageId;
                // Current assignment is persisted on the application, completed
                // stages show their recorded actor, and upcoming stages show the
                // allocation-rule preview. The legacy stage UserId is never used.
                var assignedUserId = isCurrentStage
                    ? (application.UserId is > 0 ? application.UserId : null)
                    : history != null && IsCompletedStatus(history.Status)
                        ? history.ActionByUserId
                        : futureStageUserIds.GetValueOrDefault(stage.Id);
                users.TryGetValue(assignedUserId ?? 0, out var user);

                return new ApplicationApprovalStageDto
                {
                    ApprovalFlowStageId = stage.Id,
                    StageOrder = stage.StageOrder,
                    StageName = stage.StageName,
                    Status = history?.Status ?? ApplicationStatus.Pending,
                    Remark = history?.Remark,
                    CreatedDate = history?.CreatedDate,
                    UserName = user?.UserName,
                    FirstName = user?.FirstName,
                    LastName = user?.LastName,
                    IsCurrentStage = isCurrentStage
                };
            }).ToList()
        };

        result.TotalApprovalStages = result.ApprovalStages.Count;
        result.CompletedStages = result.ApprovalStages.Count(stage => IsCompletedStatus(stage.Status));
        return result;
    }


    public async Task<RTSApplicationViewDetailsDto?> ViewApplicationApprovalSummaryAsync(int applicationId, CancellationToken cancellationToken = default)
    {
        var result = await _repository
            .GetQueryable()
            .AsNoTracking()
            .Where(x =>
                !x.MarkedForDeletion &&
                x.Id == applicationId)
            .Select(x => new RTSApplicationViewDetailsDto
            {
                ApplicationId = x.Id,
                ApplicationNo = x.ApplicationNo,
                ServiceId = x.ServiceId,
                ServiceName = x.Service != null ? x.Service.ServiceName : null,
                DepartmentId = x.DepartmentId,
                DepartmentName = x.Department != null ? x.Department.DepartmentName : null,
                ApplicationStatus = x.ApplicationStatus,
                Remark = x.Remark,
                IsCertificateRequired = x.Service != null && x.Service.IsCertificateRequired,
                CertificateType = x.Service != null ? (byte)x.Service.CertificateType : (byte)0,
                IssuedCertificateGuid = x.IssuedCertificateGuid,
                Documents = x.FieldValueData
                    .Where(fv =>
                        !fv.MarkedForDeletion &&
                        fv.FieldDefinition != null &&
                        !fv.FieldDefinition.MarkedForDeletion &&
                        fv.FieldDefinition.FieldGroup != null &&
                        fv.FieldDefinition.FieldGroup == "Document Uploads")
                         .OrderBy(fv =>
                        fv.FieldDefinition!.DisplayOrder)
                    .Select(fv => new ApplicationDocumentDto
                    {
                        FieldDefinitionId = fv.FieldDefinitionId,
                        DocumentName = fv.FieldDefinition!.FieldLabel,
                        DocumentNameLocal=fv.FieldDefinition.FieldLabelLocal,
                        DocumentGuid = fv.DocumentGuid,
                        Value=fv.TextValue,
                        IsRequired = fv.FieldDefinition.IsRequired,
                        IsUploaded = fv.DocumentGuid.HasValue
                    })
                    .ToList(),
                ApplicationDetails = x.FieldValueData
                    .Where(fv =>
                        !fv.MarkedForDeletion &&
                        fv.FieldDefinition != null &&
                        !fv.FieldDefinition.MarkedForDeletion &&
                        fv.FieldDefinition.FieldGroup != "Document Uploads")
                    .OrderBy(fv => fv.FieldDefinition!.DisplayOrder)
                    .Select(fv => new ApplicationFieldValueDto
                    {
                        FieldDefinitionId = fv.FieldDefinitionId,
                        FieldCode = fv.FieldDefinition!.FieldCode,
                        FieldLabel = fv.FieldDefinition.FieldLabel,
                        FieldLabelLocal = fv.FieldDefinition.FieldLabelLocal,
                        FieldType = fv.FieldDefinition.FieldType,
                        FieldGroup = fv.FieldDefinition.FieldGroup,
                        DisplayOrder = fv.FieldDefinition.DisplayOrder,
                        IsRequired = fv.FieldDefinition.IsRequired,

                        Value =
                            fv.TextValue != null
                                ? fv.TextValue
                                : fv.NumberValue != null
                                    ? fv.NumberValue.Value.ToString()
                                    : fv.DateValue != null
                                        ? fv.DateValue.Value.ToString("yyyy-MM-dd")
                                        : fv.BooleanValue != null
                                            ? fv.BooleanValue.Value.ToString()
                                            : null
                    })
                    .ToList()

            }).SingleOrDefaultAsync(cancellationToken);


        if (result == null)
        {
            return null;
        }

        return result;
    }

    // <summary>
    // Get the current approval officer for a given application. and it Access and Name Role and Email and Stage Details 
    // <summary>

    public async Task<CurrentApprovalOfficerDto?> GetCurrentApprovalOfficerAsync(
    int applicationId,
    CancellationToken cancellationToken = default)
    {
        if (applicationId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(applicationId),
                "Invalid application ID.");
        }

        var result = await _repository
            .GetQueryable()
            .AsNoTracking()
            .Where(application =>
                application.Id == applicationId &&
                application.IsActive &&
                !application.MarkedForDeletion)
            .Select(application => new
            {
                ApplicationId = application.Id,
                application.ApplicationNo,
                application.ApplicationStatus,
                application.ApprovalFlowId,
                application.CurrentApprovalFlowStageId,
                application.UserId,
                application.ServiceId,
                IsPaymentAfterApproval = application.Service.IsPaymentAfterApproval,
                ServiceName = application.Service != null ? application.Service.ServiceName : null,
                ServiceFees = application.Service != null ? application.Service.Fees : null,
                FeesRequired = application.Service != null && application.Service.FeesRequired,
                CertificateType = application.Service != null ? application.Service.CertificateType : NtisPlatform.Core.Enums.RTSCertificateType.None,
                IsCertificateRequired = application.Service == null || application.Service.IsCertificateRequired
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (result == null)
        {
            return null;
        }

        var isCertificateIssued = await _issuedCertificateRepository   //checks the applications Certficate is issued or not
         .GetQueryable()
         .AsNoTracking()
         .AnyAsync(
         x =>
         x.ApplicationId == applicationId &&
         x.IsActive &&
         !x.MarkedForDeletion,
            cancellationToken);

        // Application workflow is already completed.
        bool requiresCertificate = result.IsCertificateRequired && result.CertificateType != NtisPlatform.Core.Enums.RTSCertificateType.None;
        if (result.ApplicationStatus == ApplicationStatus.Approved && (isCertificateIssued || !requiresCertificate))
        {
            return null;
        }
        if (result.ApprovalFlowId == 0 || result.CurrentApprovalFlowStageId == 0)
        {
            throw new InvalidOperationException(
                "Current workflow stage is not available.");
        }

        var currentStage = await _approvalFlowStageRepository
            .GetQueryable()
            .AsNoTracking()
            .Where(stage =>
                stage.Id == result.CurrentApprovalFlowStageId &&
                stage.ApprovalFlowId == result.ApprovalFlowId)
            .Select(stage => new
            {
                stage.Id,
                stage.ApprovalFlowId,
                stage.StageOrder,
                stage.StageName,
                stage.SLADays,
                stage.CanVerifyDocument,
                stage.CanApprove,
                stage.CanReject,
                stage.CanReturn,
                stage.CanPay,
                stage.CanEdit,
                stage.CanViewNoteSheet,
                stage.CanIssueCertificate,
                stage.CanViewCertificate,
                stage.CanUploadDocument,
                stage.IsFinalStage
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (currentStage == null)
        {
            throw new InvalidOperationException(
                "Current approval stage configuration was not found.");
        }

        var officer = await _userRepository
            .GetQueryable()
            .AsNoTracking()
            .Where(user => result.UserId.HasValue && user.Id == result.UserId.Value &&
                user.IsActive && !user.MarkedForDeletion)
            .Select(user => new
            {
                user.Id,
                user.UserName,
                user.FirstName,
                user.LastName,
                user.Email
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (officer == null)
        {
            throw new InvalidOperationException(
                $"The application has no active assigned officer for stage '{currentStage.StageName}'.");
        }

        var demand = await _demandRepository.GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.ApplicationId == result.ApplicationId && d.IsActive, cancellationToken);

        var paymentTxn = await _paymentRepository.GetQueryable()
            .AsNoTracking()
            .Include(p => p.PaymentStatus)
            .Where(p => p.ApplicationId == result.ApplicationId && p.IsActive && !p.IsReceiptCancelled)
            .OrderByDescending(p => p.Id)
            .FirstOrDefaultAsync(cancellationToken);

        bool isPaid = string.Equals(demand?.DemandStatus, "Paid", StringComparison.OrdinalIgnoreCase) ||
                      string.Equals(paymentTxn?.PaymentStatus?.StatusCode, "SUCCESS", StringComparison.OrdinalIgnoreCase);

        decimal fee = demand?.TotalAmount ?? paymentTxn?.TotalAmount ?? result.ServiceFees ?? 0m;
        string paymentStatus = isPaid
            ? "SUCCESS"
            : (paymentTxn?.PaymentStatus?.StatusCode ?? (fee > 0 ? "PENDING" : "NOT_REQUIRED"));
        string? receiptNo = paymentTxn?.ReceiptNo;

        return new CurrentApprovalOfficerDto
        {
            ApplicationId = result.ApplicationId,
            ApplicationNo = result.ApplicationNo,
            ApplicationStatus = result.ApplicationStatus,

            ApprovalFlowId = currentStage.ApprovalFlowId,
            StageId = currentStage.Id,
            StageName = currentStage.StageName,
            StageOrder = currentStage.StageOrder,
            SLADays = currentStage.SLADays,
            IsFinalStage = currentStage.IsFinalStage,

            OfficerId = officer.Id,
            FirstName = officer.FirstName,
            LastName = officer.LastName,
            OfficerName = officer.UserName,
            OfficerEmail = officer.Email,

            CanVerifyDocument = currentStage.CanVerifyDocument,
            CanApprove = currentStage.CanApprove,
            CanReject = currentStage.CanReject,
            CanReturn = currentStage.CanReturn,
            CanPay = currentStage.CanPay,
            CanEdit = currentStage.CanEdit,
            CanViewNoteSheet = currentStage.CanViewNoteSheet,
            CanIssueCertificate = requiresCertificate && currentStage.CanIssueCertificate,
            CanViewCertificate = requiresCertificate && currentStage.CanViewCertificate,
            CanUploadDocument = currentStage.CanUploadDocument,
            IsManualCertificate = requiresCertificate && result.CertificateType == NtisPlatform.Core.Enums.RTSCertificateType.Manual,
            CertificateType = (int)result.CertificateType,
            IsCertificateRequired = result.IsCertificateRequired,

            ServiceId = result.ServiceId,
            ServiceName = result.ServiceName,
            ServiceFees = result.ServiceFees,
            IsPaymentAfterApproval=result.IsPaymentAfterApproval,
            FeesRequired = result.FeesRequired || fee > 0,
            PaymentTransactionId = paymentTxn?.Id,
            PayableFee = fee,
            HasPaymentTransaction = paymentTxn != null || (demand != null && demand.TotalAmount > 0) || fee > 0,
            IsPaid = isPaid,
            PaymentStatus = paymentStatus,
            ReceiptNo = receiptNo
        };
    }

    // <summary>
    // Verify the documents for a given application and process it to the next approval stage if applicable. By Clerk Screen
    // </summary>
    public async Task<RTSApplicationApprovalResponseDto> VerifyDocumentsAndProcessApplicationAsync(
    int applicationId,
    UpdateRTSApplicationProcessDto dto,
    CancellationToken cancellationToken = default)
    {
        var application = await _repository
            .GetQueryable()
            .Include(x => x.Service)
            .FirstOrDefaultAsync(x =>
                x.Id == applicationId && x.IsActive && !x.MarkedForDeletion,
                cancellationToken);

        if (application == null)
            throw new InvalidOperationException("Application not found.");

        var currentStage = await _approvalFlowStageRepository
            .GetQueryable()
            .AsNoTracking()
            .Where(stage => stage.Id == application.CurrentApprovalFlowStageId)
            .Select(stage => new {
                stage.CanVerifyDocument,
                stage.StageName,
                stage.Id
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (currentStage == null)
            throw new InvalidOperationException("Current approval stage was not found.");

        if (!currentStage.CanVerifyDocument)
            throw new InvalidOperationException("This stage does not permit document verification.");

        // The transaction snapshot, not today's Service Master fee, is authoritative.
        await EnsurePaymentSnapshotIsSettledAsync(
            applicationId,
            application.ApplicationNo,
            "process",
            cancellationToken);

        var currentHistory = await _historyRepository
        .GetQueryable()
        .Where(history =>
        history.ApplicationId == applicationId &&
        history.ApprovalFlowId == application.ApprovalFlowId &&
        history.ApprovalFlowStageId == application.CurrentApprovalFlowStageId &&
        history.IsActive)
        .OrderByDescending(history => history.Id)
        .FirstOrDefaultAsync(cancellationToken);

        if (currentHistory == null)
        {
            throw new InvalidOperationException("Current stage history record was not found.");
        }

        // Advance to next stage or update status
        var nextStage = await _approvalFlowStageRepository
            .GetQueryable()
            .AsNoTracking()
            .Where(stage =>
                stage.ApprovalFlowId == application.ApprovalFlowId &&
                stage.StageOrder > application.CurrentStageOrder)
            .OrderBy(stage => stage.StageOrder)
            .Select(stage => new { StageId = stage.Id, stage.StageOrder, stage.StageName })
            .FirstOrDefaultAsync(cancellationToken);

        if (nextStage == null)
            throw new InvalidOperationException("Next approval stage is not configured.");

        //this Update ApplicationDetails Table
        application.CurrentApprovalFlowStageId = nextStage.StageId;
        application.CurrentStageOrder = nextStage.StageOrder;
        application.UserId = await ResolveStageOfficerAsync(application, nextStage.StageId, cancellationToken);
        application.ApplicationStatus = ApplicationStatus.DocumentVerified;
        application.Remark = dto.Remark;
        application.IsReverted = false;
        application.UpdatedBy = dto.UpdatedBy;
        application.UpdatedDate = DateTime.Now;

        //this Insert History Table for Next Officer
        application.TrackApplicationHistory.Add(new TrackApplicationHistoryEntity
        {
            ApprovalFlowId = application.ApprovalFlowId,
            ApprovalFlowStageId = currentStage.Id,
            ActionByUserId = dto.UpdatedBy,
            Status = ApplicationStatus.DocumentVerified,
            Action = $"{ApplicationStatus.DocumentVerified} by {currentStage.StageName}",
            Remark = dto.Remark,
            IsReverted = false,
            IsActive = true,
            CreatedBy = dto.UpdatedBy
        });

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            var (mobile, name, serviceName) = await GetApplicationSmsDetailsAsync(application.Id, application.ServiceId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(mobile))
            {
                await _smsNotificationService.SendApplicationStageAdvancedAsync(
                    application.Id,
                    application.ApplicationNo ?? $"APP{application.Id}",
                    name,
                    mobile,
                    serviceName,
                    nextStage.StageName,
                    application.ApplicationStatus,
                    dto.Remark,
                    cancellationToken);
            }
        }
        catch { }

        return new RTSApplicationApprovalResponseDto
        {
            ApplicationId = application.Id,
            Status = application.ApplicationStatus,
            ApplicationNo = application.ApplicationNo,
            Remark = application.Remark
        };
    }

    public async Task<RTSApplicationApprovalResponseDto> VerifyApplicationAndSentToApproveAsync(
    int applicationId,
    UpdateRTSApplicationProcessDto dto,
    CancellationToken cancellationToken = default)
    {
        var application = await _repository.GetQueryable()
            .Include(x => x.Service)
            .FirstOrDefaultAsync(x => x.Id == applicationId && x.IsActive && !x.MarkedForDeletion,
            cancellationToken);

        if (application == null)
            throw new InvalidOperationException("Application not found.");

        var currentStage = await _approvalFlowStageRepository
            .GetQueryable()
            .AsNoTracking()
            .Where(stage => stage.Id == application.CurrentApprovalFlowStageId)
            .Select(stage => new
            {
                stage.CanApprove,
                stage.StageName,
                stage.IsFinalStage,
                stage.Id,
                stage.StageOrder

            })
            .FirstOrDefaultAsync(cancellationToken);


        if (currentStage == null)
            throw new InvalidOperationException("Current approval stage was not found.");

        if (!currentStage.CanApprove)
            throw new InvalidOperationException(
                $"{currentStage.StageName} does not permit application approval.");

        // The transaction snapshot, not today's Service Master fee, is authoritative.
        await EnsurePaymentSnapshotIsSettledAsync(
            applicationId,
            application.ApplicationNo,
            "approve",
            cancellationToken);

        var currentHistory = await _historyRepository
                .GetQueryable()
                .Where(history =>
                    history.ApplicationId == applicationId &&
                    history.IsActive)
                .OrderByDescending(history => history.Id)
                .FirstOrDefaultAsync(cancellationToken);

        if (currentHistory == null)
            throw new InvalidOperationException(
                "Current stage history record was not found.");

        var nextStage = await _approvalFlowStageRepository
           .GetQueryable()
           .AsNoTracking()
           .Where(stage =>
               stage.ApprovalFlowId == application.ApprovalFlowId &&
               stage.StageOrder > application.CurrentStageOrder)
           .OrderBy(stage => stage.StageOrder)
           .Select(stage => new { StageId = stage.Id, stage.StageOrder, stage.StageName })
           .FirstOrDefaultAsync(cancellationToken);

        // Finalize approval only if current stage is final and there are no subsequent stages
        if (currentStage.IsFinalStage && nextStage == null)
        {
            application.TrackApplicationHistory.Add(new TrackApplicationHistoryEntity
            {
                ApprovalFlowId = application.ApprovalFlowId,
                ApprovalFlowStageId = currentStage.Id,
                ActionByUserId = dto.UpdatedBy,
                Status = ApplicationStatus.Approved,
                Action = $"{ApplicationStatus.Approved} by {currentStage.StageName}",
                Remark = dto.Remark,
                IsReverted = false,
                IsActive = true,
                CreatedBy = dto.UpdatedBy
            });


            //No Need to Assign Next Officier IF Application Is Rejected Or Approved
            application.UserId = dto.UpdatedBy;
            application.ApplicationStatus = ApplicationStatus.Approved;
            application.Remark = dto.Remark;
            application.IsReverted = false;
            application.IssuedCertificateGuid = dto.IssuedCertificateGuid;
            application.UpdatedBy = dto.UpdatedBy;
            application.UpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            try
            {
                if (!string.IsNullOrWhiteSpace(application.ApplicationNo))
                {
                    await _aapleSarkarService.UpdateStatusAsync(application.ApplicationNo, application.ApplicationStatus, remark: null, ct: cancellationToken);
                }
            }
            catch { }

            try
            {
                var (mobile, name, serviceName) = await GetApplicationSmsDetailsAsync(application.Id, application.ServiceId, cancellationToken);
                if (!string.IsNullOrWhiteSpace(mobile))
                {
                    await _smsNotificationService.SendApplicationApprovedAsync(
                        application.Id,
                        application.ApplicationNo ?? $"APP{application.Id}",
                        name,
                        mobile,
                        serviceName,
                        cancellationToken);
                }
            }
            catch { }

            return new RTSApplicationApprovalResponseDto
            {
                ApplicationId = application.Id,
                ApplicationNo = application.ApplicationNo,
                Status = application.ApplicationStatus,
                Remark = application.Remark
            };
        }

        if (nextStage == null)
            throw new InvalidOperationException("Next approval stage is not configured.");


        application.CurrentApprovalFlowStageId = nextStage.StageId;
        application.CurrentStageOrder = nextStage.StageOrder;
        application.UserId = await ResolveStageOfficerAsync(application, nextStage.StageId, cancellationToken);
        application.ApplicationStatus = ApplicationStatus.ApplicationVerified;
        application.Remark = dto.Remark;
        application.IsReverted = false;
        application.IssuedCertificateGuid = dto.IssuedCertificateGuid;
        application.UpdatedBy = dto.UpdatedBy;
        application.UpdatedDate = DateTime.Now;

        application.TrackApplicationHistory.Add(new TrackApplicationHistoryEntity
        {
            ApprovalFlowId = application.ApprovalFlowId,
            ApprovalFlowStageId = currentStage.Id,
            ActionByUserId = dto.UpdatedBy,
            Status = ApplicationStatus.ApplicationVerified,
            Action = $"{ApplicationStatus.ApplicationVerified} by {currentStage.StageName}",
            Remark = dto.Remark,
            IsReverted = false,
            IsActive = true,
            CreatedBy = dto.UpdatedBy
        });

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            var (mobile, name, serviceName) = await GetApplicationSmsDetailsAsync(application.Id, application.ServiceId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(mobile))
            {
                await _smsNotificationService.SendApplicationStageAdvancedAsync(
                    application.Id,
                    application.ApplicationNo ?? $"APP{application.Id}",
                    name,
                    mobile,
                    serviceName,
                    nextStage.StageName,
                    application.ApplicationStatus,
                    dto.Remark,
                    cancellationToken);
            }
        }
        catch { }

        return new RTSApplicationApprovalResponseDto
        {
            ApplicationId = application.Id,
            Status = application.ApplicationStatus,
            ApplicationNo = application.ApplicationNo,
            Remark = application.Remark
        };
    }

    public async Task<RTSApplicationApprovalResponseDto> RejectApplicationByOfficerAsync(
    int applicationId,
    UpdateRTSApplicationProcessDto dto,
    CancellationToken cancellationToken = default)
    {
        var application = await _repository.GetQueryable()
            .SingleOrDefaultAsync (x => x.Id == applicationId && x.IsActive && !x.MarkedForDeletion,
            cancellationToken);

        if (application == null)
            throw new InvalidOperationException("Application not found.");

        var currentStage = await _approvalFlowStageRepository
            .GetQueryable()
            .AsNoTracking()
            .Where(stage => stage.Id == application.CurrentApprovalFlowStageId)
            .Select(stage => new
            {
                stage.CanReject,
                stage.StageName,
                stage.IsFinalStage,
                stage.Id,
                stage.StageOrder
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (currentStage == null)
            throw new InvalidOperationException("Current approval stage was not found.");

        if (!currentStage.CanReject)
            throw new InvalidOperationException(
                $"{currentStage.StageName} does not permit application Reject For This Officer");

        var currentHistory = await _historyRepository
                .GetQueryable()
                .Where(history =>
                    history.ApplicationId == applicationId &&
                    history.IsActive)
                .OrderByDescending(history => history.Id)
                .FirstOrDefaultAsync(cancellationToken);


        if (currentHistory == null)
            throw new InvalidOperationException("Current stage history record was not found.");

            application.TrackApplicationHistory.Add(new TrackApplicationHistoryEntity
            {
                ApprovalFlowId = application.ApprovalFlowId,
                ApprovalFlowStageId = currentStage.Id,
                ActionByUserId = dto.UpdatedBy,
                Status = ApplicationStatus.Rejected,
                Action = $"{ApplicationStatus.Rejected} by {currentStage.StageName}",
                Remark = dto.Remark,
                IsReverted = false,
                IsActive = true,
                CreatedBy = dto.UpdatedBy
            });

        //No Need to Assign Next Officier IF Application Is Rejevted Or Approved
            application.UserId = dto.UpdatedBy;
            application.ApplicationStatus = ApplicationStatus.Rejected;
            application.Remark = dto.Remark;
            application.IsReverted = false;
            application.UpdatedBy = dto.UpdatedBy;
            application.UpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            try
            {
                if (!string.IsNullOrWhiteSpace(application.ApplicationNo))
                {
                    await _aapleSarkarService.UpdateStatusAsync(application.ApplicationNo, application.ApplicationStatus, remark: dto.Remark, ct: cancellationToken);
                }
            }
            catch { }

            try
            {
                var (mobile, name, serviceName) = await GetApplicationSmsDetailsAsync(application.Id, application.ServiceId, cancellationToken);
                if (!string.IsNullOrWhiteSpace(mobile))
                {
                    await _smsNotificationService.SendApplicationRejectedAsync(
                        application.Id,
                        application.ApplicationNo ?? $"APP{application.Id}",
                        name,
                        mobile,
                        serviceName,
                        dto.Remark,
                        cancellationToken);
                }
            }
            catch { }

            return new RTSApplicationApprovalResponseDto
            {
                ApplicationId = application.Id,
                ApplicationNo = application.ApplicationNo,
                Status = application.ApplicationStatus,
                Remark = application.Remark
            };
    }

    public async Task<RTSApplicationApprovalResponseDto> VerifyAndCorrectApplicationAsync(
    int applicationId,
    UpdateRTSApplicationVerificationDto dto,
    CancellationToken cancellationToken = default)
    {
        var application = await _repository
            .GetQueryable()
            .FirstOrDefaultAsync(x => x.Id == applicationId && x.IsActive &&!x.MarkedForDeletion,
                cancellationToken);

        if (application == null)
            throw new InvalidOperationException("Application not found.");

        var currentStage = await _approvalFlowStageRepository
        .GetQueryable()
        .AsNoTracking()
        .Where(x => x.Id == application.CurrentApprovalFlowStageId)
        .Select(Stage => new
        {
            Stage.StageName,
            Stage.CanEdit
        })
        .FirstOrDefaultAsync(cancellationToken);

        if (currentStage == null)
            throw new InvalidOperationException("Current approval stage was not found.");

        if (!currentStage.CanEdit && application.ApplicationStatus != ApplicationStatus.Reverted && !application.IsReverted)
            throw new InvalidOperationException(
                $"{currentStage.StageName} does not permit application correction.");

        var fieldValues = await _fieldValueRepository
            .GetQueryable()
            .Where(x =>
                x.ApplicationId == applicationId &&
                x.IsActive &&
                !x.MarkedForDeletion)
            .ToListAsync(cancellationToken);

        foreach (var item in dto.FieldValue)
        {
            var fieldValue = fieldValues.FirstOrDefault(x =>
                x.ApplicationId == applicationId &&
                x.FieldDefinitionId == item.FieldDefinitionId);

            if (fieldValue == null)
            {
                var newFieldValue = new RTSFieldValueEntity
                {
                    ApplicationId = applicationId,
                    FieldDefinitionId = item.FieldDefinitionId,
                    TextValue = item.TextValue,
                    NumberValue = item.NumberValue,
                    DateValue = item.DateValue,
                    BooleanValue = item.BooleanValue,
                    DocumentGuid = item.DocumentGuid,
                    IsActive = true,
                    CreatedBy = dto.UpdatedBy,
                    CreatedDate = DateTime.Now
                };
                await _fieldValueRepository.AddAsync(newFieldValue, cancellationToken);
            }
            else
            {
                fieldValue.TextValue = item.TextValue;
                fieldValue.NumberValue = item.NumberValue;
                fieldValue.DateValue = item.DateValue;
                fieldValue.BooleanValue = item.BooleanValue;
                fieldValue.DocumentGuid = item.DocumentGuid;
                fieldValue.UpdatedBy = dto.UpdatedBy;
                fieldValue.UpdatedDate = DateTime.Now;
                await _fieldValueRepository.UpdateAsync(fieldValue, cancellationToken);
            }
        }

        var wasReverted = application.IsReverted || application.ApplicationStatus == ApplicationStatus.Reverted;
        if (wasReverted)
        {
            application.ApplicationStatus = ApplicationStatus.Pending;
            application.IsReverted = false;
        }

        application.Remark = dto.Remark;
        application.UpdatedBy = dto.UpdatedBy;
        application.UpdatedDate = DateTime.Now;

        var history = new TrackApplicationHistoryEntity
        {
            ApplicationId = applicationId,
            ApprovalFlowId = application.ApprovalFlowId,
            ApprovalFlowStageId = application.CurrentApprovalFlowStageId,
            ActionByUserId = dto.UpdatedBy,
            Status = wasReverted ? ApplicationStatus.Pending : ApplicationStatus.Correction,
            Action = wasReverted ? "Application Corrected & Resubmitted" : $"{ApplicationStatus.Correction} at {currentStage.StageName}",
            Remark = dto.Remark ?? (wasReverted ? "Corrections updated and resubmitted" : null),
            IsReverted = false,
            IsActive = true,
            CreatedBy = dto.UpdatedBy,
            CreatedDate = DateTime.Now
        };

        await _historyRepository.AddAsync(history, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            var (mobile, name, serviceName) = await GetApplicationSmsDetailsAsync(application.Id, application.ServiceId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(mobile))
            {
                var statusText = wasReverted ? "RESUBMITTED" : "CORRECTED";
                await _smsNotificationService.SendApplicationStatusUpdateAsync(
                    application.Id,
                    application.ApplicationNo ?? $"APP{application.Id}",
                    name,
                    mobile,
                    serviceName,
                    statusText,
                    null,
                    cancellationToken);
            }
        }
        catch { }

        return new RTSApplicationApprovalResponseDto
        {
            ApplicationId = application.Id,
            Status = application.ApplicationStatus,
            ApplicationNo = application.ApplicationNo,
            Remark = application.Remark
        };
    }


    /// <summary>
    /// Revert application to the previous officer/stage in the approval flow.
    /// If the current stage is the first stage, the application is reverted to the citizen.
    /// </summary>
    public async Task<RTSApplicationApprovalResponseDto> VerifyAndRevertApplicationAsync(
        int applicationId,
        UpdateRTSApplicationProcessDto dto,
        CancellationToken cancellationToken = default)
    {
        var application = await _repository.GetQueryable()
            .Where(x => x.Id == applicationId && x.IsActive && !x.MarkedForDeletion)
            .FirstOrDefaultAsync(cancellationToken);

        if (application == null)
            throw new InvalidOperationException("Application not found.");

        var currentStage = await _approvalFlowStageRepository
            .GetQueryable()
            .AsNoTracking()
            .Where(stage => stage.Id == application.CurrentApprovalFlowStageId)
            .Select(stage => new
            {
                stage.CanReturn,
                stage.StageName,
                stage.IsFinalStage,
                stage.Id,
                stage.StageOrder,
                stage.ApprovalFlowId
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (currentStage == null)
            throw new InvalidOperationException("No Approval Stage Found");

        if (!currentStage.CanReturn)
        {
            throw new InvalidOperationException($"{currentStage.StageName} does not permit application correction.");
        }

        // Find the previous stage in the approval flow (officer-wise revert)
        var previousStage = await _approvalFlowStageRepository
            .GetQueryable()
            .AsNoTracking()
            .Where(stage =>
                stage.ApprovalFlowId == currentStage.ApprovalFlowId &&
                stage.StageOrder < currentStage.StageOrder)
            .OrderByDescending(stage => stage.StageOrder)
            .Select(stage => new
            {
                StageId = stage.Id,
                stage.StageOrder,
                stage.StageName
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (previousStage != null)
        {
            // Officer-wise revert: move application back to the previous stage/officer
            application.CurrentApprovalFlowStageId = previousStage.StageId;
            application.CurrentStageOrder = previousStage.StageOrder;
            application.UserId = await ResolveStageOfficerAsync(application, previousStage.StageId, cancellationToken);
            application.ApplicationStatus = ApplicationStatus.Reverted;
            application.Remark = dto.Remark;
            application.IsReverted = true;
            application.UpdatedBy = dto.UpdatedBy;
            application.UpdatedDate = DateTime.Now;

            // Create a new pending history entry for the previous officer
            application.TrackApplicationHistory.Add(new TrackApplicationHistoryEntity
            {
                ApprovalFlowId = application.ApprovalFlowId,
                ApprovalFlowStageId = currentStage.Id,
                ActionByUserId = dto.UpdatedBy,
                Status = ApplicationStatus.Reverted,
                Action = $"{ApplicationStatus.Reverted} by {currentStage.StageName}",
                Remark = dto.Remark,
                IsReverted = true,
                IsActive = true,
                CreatedBy = dto.UpdatedBy
            });
        }
        else
        {

            var isAlreadyRevertedToCitizen = await _historyRepository
            .GetQueryable()
            .AnyAsync(x =>
            x.ApplicationId == application.Id &&
            x.ApprovalFlowStageId == currentStage.Id &&
            x.IsReverted &&
            x.Status == ApplicationStatus.Reverted &&
            x.IsActive,
            cancellationToken);

            if (application.IsReverted == true && isAlreadyRevertedToCitizen)
            {
                throw new InvalidOperationException("Application is already reverted to citizen.");
            }

            // First stage — reverted By Clerk to citizen (no previous officer)
            application.ApplicationStatus = ApplicationStatus.Reverted;
            application.Remark = dto.Remark;
            application.IsReverted = true;
            application.UpdatedBy = dto.UpdatedBy;
            application.UpdatedDate = DateTime.Now;


            await _historyRepository.AddAsync(new TrackApplicationHistoryEntity
            {
                ApplicationId = application.Id,
                ApprovalFlowId = currentStage.ApprovalFlowId,
                ApprovalFlowStageId = currentStage.Id,
                ActionByUserId = dto.UpdatedBy,
                Action = $"{ApplicationStatus.Reverted} by {currentStage.StageName}",
                Status = ApplicationStatus.Reverted,
                Remark = dto.Remark,
                IsReverted = true,
                IsActive = true,
                CreatedBy = dto.UpdatedBy,
                CreatedDate = DateTime.Now
            }, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            var (mobile, name, serviceName) = await GetApplicationSmsDetailsAsync(application.Id, application.ServiceId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(mobile))
            {
                await _smsNotificationService.SendApplicationRevertedAsync(
                    application.Id,
                    application.ApplicationNo ?? $"APP{application.Id}",
                    name,
                    mobile,
                    serviceName,
                    dto.Remark,
                    cancellationToken);
            }
        }
        catch { }

        return new RTSApplicationApprovalResponseDto
        {
            ApplicationId = application.Id,
            ApplicationNo = application.ApplicationNo,
            Status = application.ApplicationStatus,
            Remark = application.Remark
        };
    }

    private async Task<(string? mobile, string name, string serviceName)> GetApplicationSmsDetailsAsync(int applicationId, int serviceId, CancellationToken ct)
    {
        try
        {
            var app = await _repository.GetByIdAsync(applicationId, ct);
            string? mobile = app?.ApplicantMobileNo;

            if (string.IsNullOrWhiteSpace(mobile))
            {
                var fieldValues = await _fieldValueRepository.GetQueryable()
                    .Include(f => f.FieldDefinition)
                    .Where(f => f.ApplicationId == applicationId && !f.MarkedForDeletion && f.IsActive)
                    .ToListAsync(ct);

                foreach (var fv in fieldValues)
                {
                    var code = (fv.FieldDefinition?.FieldCode ?? string.Empty).ToLowerInvariant();
                    var label = (fv.FieldDefinition?.FieldLabel ?? string.Empty).ToLowerInvariant();
                    var val = fv.TextValue?.Trim();

                    if (string.IsNullOrWhiteSpace(val)) continue;

                    if (string.IsNullOrWhiteSpace(mobile) &&
                        (code.Contains("mobile") || code.Contains("phone") || code.Contains("contact") ||
                         label.Contains("mobile") || label.Contains("मोबाईल") || label.Contains("फोन")))
                    {
                        mobile = val;
                        break;
                    }
                }
            }

            var svc = await _serviceRepository.GetByIdAsync(serviceId, ct);
            var serviceName = svc?.ServiceName ?? "RTS Service";

            return (mobile, "Citizen", serviceName);
        }
        catch
        {
            return (null, "Citizen", "RTS Service");
        }
    }






    // <summary>
    // Helper Methods
    private static bool IsCompletedStatus(string? status)
    {
        return status != null &&
        (
            status.Equals(ApplicationStatus.Approved, StringComparison.OrdinalIgnoreCase) ||
            status.Equals(ApplicationStatus.Rejected, StringComparison.OrdinalIgnoreCase) ||
            status.Equals(ApplicationStatus.DocumentVerified, StringComparison.OrdinalIgnoreCase)||
            status.Equals(ApplicationStatus.ApplicationVerified, StringComparison.OrdinalIgnoreCase)
        );
    }

    private static int? ExtractSlaDays(string? sla)  //getApplication Dashboard to find remaining days
    {
        var numericPart = new string(sla?.Where(char.IsDigit).ToArray() ?? []);
        return int.TryParse(numericPart, out var days) ? days : null;
    }

    public async Task<List<NtisPlatform.Application.DTOs.RTSTrackApplicationHistory.RTSTrackApplicationHistoryDto>> GetTrackApplicationHistoryAsync(
        int applicationId,
        CancellationToken cancellationToken = default)
    {
        var histories = await _historyRepository.GetQueryable()
            .AsNoTracking()
            .Include(h => h.Application)
            .Include(h => h.ApprovalFlowStage)
            .Where(h => h.ApplicationId == applicationId && h.IsActive)
            .OrderBy(h => h.Id)
            .ToListAsync(cancellationToken);

        var userIds = histories.Where(h => h.ActionByUserId.HasValue).Select(h => h.ActionByUserId!.Value).Distinct().ToList();
        var users = await _userRepository.GetQueryable()
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => new { u.UserName, u.FirstName, u.LastName }, cancellationToken);

        var result = new List<NtisPlatform.Application.DTOs.RTSTrackApplicationHistory.RTSTrackApplicationHistoryDto>();
        foreach (var h in histories)
        {
            string? userName = null;
            string? officerName = null;
            if (h.ActionByUserId.HasValue && users.TryGetValue(h.ActionByUserId.Value, out var user))
            {
                userName = user.UserName;
                officerName = $"{user.FirstName} {user.LastName}".Trim();
                if (string.IsNullOrWhiteSpace(officerName)) officerName = user.UserName;
            }

            bool isDsc = h.Action != null && (
                h.Action.Contains("DigitalSign", StringComparison.OrdinalIgnoreCase) ||
                h.Action.Contains("DSC", StringComparison.OrdinalIgnoreCase) ||
                (h.Remark != null && h.Remark.Contains("DSC Hash", StringComparison.OrdinalIgnoreCase))
            );

            result.Add(new NtisPlatform.Application.DTOs.RTSTrackApplicationHistory.RTSTrackApplicationHistoryDto
            {
                Id = h.Id,
                ApplicationId = h.ApplicationId,
                ApplicationNo = h.Application?.ApplicationNo,
                ApprovalFlowId = h.ApprovalFlowId,
                ApprovalFlowStageId = h.ApprovalFlowStageId,
                StageName = h.ApprovalFlowStage?.StageName,
                ActionByUserId = h.ActionByUserId,
                ActionByUserName = userName,
                ActionByOfficerName = officerName,
                Action = h.Action ?? string.Empty,
                Status = h.Status,
                Remark = h.Remark,
                IsReverted = h.IsReverted,
                IsDigitallySigned = isDsc,
                DigitalSignatureInfo = isDsc ? h.Remark : null,
                CreatedDate = h.CreatedDate ?? DateTime.UtcNow
            });
        }

        return result;
    }

    public async Task<IReadOnlyList<RTSStageDocumentDto>> GetStageDocumentsAsync(int applicationId, CancellationToken cancellationToken = default)
    {
        var bindings = await _bindingRepository.GetQueryable()
            .AsNoTracking()
            .Include(b => b.Document)
            .Where(b => b.ReferenceTableName == "ApplicationDetails"
                     && b.ReferenceTableId == applicationId
                     && b.BindingPurpose == "StageScrutiny"
                     && b.IsActive && !b.MarkedForDeletion
                     && b.Document != null && b.Document.IsActive && !b.Document.MarkedForDeletion)
            .OrderByDescending(b => b.CreatedDate)
            .ToListAsync(cancellationToken);

        if (bindings.Count == 0)
            return Array.Empty<RTSStageDocumentDto>();

        var userIds = bindings.Select(b => b.Document!.UploadedByUserId).Distinct().ToList();
        var users = await _userRepository.GetQueryable()
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => new
            {
                u.UserName,
                FullName = ($"{u.FirstName ?? ""} {u.LastName ?? ""}").Trim()
            }, cancellationToken);

        return bindings.Select(b =>
        {
            var doc = b.Document!;
            users.TryGetValue(doc.UploadedByUserId, out var userInfo);
            return new RTSStageDocumentDto
            {
                BindingId = b.Id,
                DocumentId = doc.Id,
                DocumentGuid = doc.DocumentGuid,
                DocumentTitle = !string.IsNullOrWhiteSpace(doc.DocumentTitle) ? doc.DocumentTitle : doc.OriginalFileName,
                FileName = doc.FileName,
                OriginalFileName = doc.OriginalFileName,
                FileSizeBytes = doc.FileSizeBytes,
                Remarks = doc.Description,
                UploadedByUserId = doc.UploadedByUserId,
                UploadedByUserName = userInfo?.UserName,
                UploadedByFullName = !string.IsNullOrWhiteSpace(userInfo?.FullName) ? userInfo.FullName : userInfo?.UserName,
                UploadedDate = doc.CreatedDate
            };
        }).ToList();
    }

    public async Task<RTSStageDocumentDto> UploadStageDocumentAsync(
        int applicationId,
        Stream fileStream,
        string fileName,
        string contentType,
        long fileLength,
        string? documentTitle,
        string? remarks,
        int officerUserId,
        CancellationToken cancellationToken = default)
    {
        var app = await _repository.GetByIdAsync(applicationId, cancellationToken);
        if (app == null)
            throw new KeyNotFoundException($"Application with ID {applicationId} was not found.");

        var currentOfficer = await GetCurrentApprovalOfficerAsync(applicationId, cancellationToken);
        if (currentOfficer != null && !currentOfficer.CanUploadDocument)
        {
            throw new InvalidOperationException("The current workflow stage does not have permission to upload scrutiny documents.");
        }

        var uploadDto = new DocumentUploadDto
        {
            DepartmentId = 5,
            ModuleId = 1005,
            ReferenceTableName = "ApplicationDetails",
            ReferenceTableId = applicationId,
            ReferencePropertyName = "Id",
            BindingPurpose = "StageScrutiny",
            DocumentType = "Stage Scrutiny Document",
            IsPrimaryDocument = false,
        };

        var uploadResult = await _documentApplicationService.UploadDocumentAsync(
            fileStream,
            fileName,
            contentType,
            fileLength,
            uploadDto,
            officerUserId,
            cancellationToken);

        var doc = await _documentRepository.GetByIdAsync(uploadResult.DocumentId, cancellationToken);
        if (doc != null)
        {
            if (!string.IsNullOrWhiteSpace(documentTitle))
                doc.DocumentTitle = documentTitle;
            if (!string.IsNullOrWhiteSpace(remarks))
                doc.Description = remarks;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var officer = await _userRepository.GetByIdAsync(officerUserId, cancellationToken);
        var fullName = officer != null ? ($"{officer.FirstName ?? ""} {officer.LastName ?? ""}").Trim() : null;

        return new RTSStageDocumentDto
        {
            BindingId = uploadResult.DocumentBindingId ?? 0,
            DocumentId = uploadResult.DocumentId,
            DocumentGuid = uploadResult.DocumentGuid,
            DocumentTitle = !string.IsNullOrWhiteSpace(documentTitle) ? documentTitle : fileName,
            FileName = uploadResult.FileName,
            OriginalFileName = fileName,
            FileSizeBytes = fileLength,
            Remarks = remarks,
            UploadedByUserId = officerUserId,
            UploadedByUserName = officer?.UserName,
            UploadedByFullName = !string.IsNullOrWhiteSpace(fullName) ? fullName : officer?.UserName,
            UploadedDate = DateTime.Now
        };
    }
}

