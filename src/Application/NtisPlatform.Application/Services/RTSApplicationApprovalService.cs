using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.Constants;
using NtisPlatform.Application.DTOs.RTSApplication;
using NtisPlatform.Application.DTOs.RTSApplicationApproval;
using NtisPlatform.Application.DTOs.RTSFieldValue;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class RTSApplicationApprovalService : BaseCommonCrudService<RTSApplicationDetailsEntity, RTSApplicationDetailsDto, CreateRTSApplicationDetailsDto, UpdateRTSFieldValueDto, RTSApplicationQueryParameters, int>, IRTSApplicationApprovalService
{
    private readonly IRepository<RTSApprovalFlowStageMasterEntity, int> _approvalFlowStageRepository;
    private readonly IRepository<UserEntity, int> _userRepository;
    private readonly IRepository<TrackApplicationHistoryEntity, int> _historyRepository;
    private readonly IRepository<RTSFieldValueEntity, int> _fieldValueRepository;
    private readonly IRepository<RTSPaymentTransactionEntity, long> _paymentRepository;
    private readonly IRepository<RTSServiceEntity, int> _serviceRepository;
    private readonly IRTSSmsNotificationService _smsNotificationService;

    public RTSApplicationApprovalService(
          IRepository<RTSApplicationDetailsEntity, int> repository,
          IRepository<RTSApprovalFlowStageMasterEntity, int> approvalFlowStageRepository,
          IRepository<TrackApplicationHistoryEntity, int> historyRepository,
          IRepository<UserEntity, int> userRepository,
          IRepository<RTSFieldValueEntity, int> fieldValueRepository,
          IRepository<RTSPaymentTransactionEntity, long> paymentRepository,
          IRepository<RTSServiceEntity, int> serviceRepository,
          IRTSSmsNotificationService smsNotificationService,
          IUnitOfWork unitOfWork,
          IMapper mapper) : base(repository, unitOfWork, mapper)
    {
        _approvalFlowStageRepository = approvalFlowStageRepository;
        _userRepository = userRepository;
        _historyRepository = historyRepository;
        _fieldValueRepository = fieldValueRepository;
        _paymentRepository = paymentRepository;
        _serviceRepository = serviceRepository;
        _smsNotificationService = smsNotificationService;
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
                    x.ApplicationStatus != ApplicationStatus.Rejected) ,
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
        var query = _repository.GetQueryable()
            .Where(x => !x.MarkedForDeletion && x.IsActive).AsQueryable();

        if (queryParameters.DepartmentId > 0)
            query = query.Where(x => x.DepartmentId == queryParameters.DepartmentId);

        if (queryParameters.ServiceId > 0)
            query = query.Where(x => x.ServiceId == queryParameters.ServiceId);

        if (!string.IsNullOrWhiteSpace(queryParameters.ApplicationNo))
            query = query.Where(x => x.ApplicationNo.Contains(queryParameters.ApplicationNo));

        if (!string.IsNullOrWhiteSpace(queryParameters.ApplicationStatus))
            query = query.Where(x => x.ApplicationStatus.Contains(queryParameters.ApplicationStatus));

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.CreatedDate)
            .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize)
            .Select(x => new RTSApplicationDashboardDetailsDto
            {
                Id = x.Id,
                DepartmentId = x.DepartmentId,
                ServiceId = x.ServiceId,
                ApplicationNo = x.ApplicationNo,
                ApplicationStatus = x.ApplicationStatus,
                CreatedDate = x.CreatedDate,
                UpdatedDate = x.UpdatedDate,
                SessionId = x.SessionId,
                OwnerId = x.OwnerId,
                DepartmentName = x.Department.DepartmentName,
                ServiceName = x.Service.ServiceName,
                Sla = x.Service.Sla,
                Remark = x.Remark,
                ApplicantName = x.ApplicantName,
                ApplicantMobileNo = x.ApplicantMobileNo,
                UserId = x.UserId,
                UserName = x.UserId != null ? x.User.UserName : null,

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
            item.RemainingDays = Math.Max(diff, 0);
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

        var result = await _repository
          .GetQueryable()
          .AsNoTracking()
          .Where(x =>
              !x.MarkedForDeletion &&
              x.Id == applicationId)
          .Select(x => new ApplicationApprovalStageDetailsDto
          {
              ApprovalStages = x.Service.ApprovalFlows
                .Where(flow => flow.IsActive)
                .OrderByDescending(flow => flow.Id)
                .Take(1)
                .SelectMany(flow => flow.ApprovalFlowStages)
                .OrderBy(stage => stage.StageOrder)
                .Select(stage => new ApplicationApprovalStageDto
                {
                    ApprovalFlowStageId = stage.Id,
                    StageOrder = stage.StageOrder,
                    StageName = stage.StageName,

                    UserName= stage.User.UserName,
                    FirstName = stage.User.FirstName,
                    LastName= stage.User.LastName,

                Status = stage.TrackApplicationHistory
                    .Where(h =>
                        h.IsActive &&
                        h.ApplicationId == applicationId)
                    .OrderByDescending(h => h.Id)
                    .Select(h => h.Status)
                    .FirstOrDefault() ?? ApplicationStatus.Pending,

                 Remark = stage.TrackApplicationHistory
                    .Where(h =>
                        h.IsActive &&
                        h.ApplicationId == applicationId)
                    .OrderByDescending(h => h.Id)
                    .Select(h => h.Remark)
                    .FirstOrDefault(),

                CreatedDate = stage.TrackApplicationHistory
                    .Where(h =>
                        h.IsActive &&
                        h.ApplicationId == applicationId)
                    .OrderByDescending(h => h.Id)
                    .Select(h => h.CreatedDate)
                    .FirstOrDefault()
                }).ToList()
          }).SingleOrDefaultAsync(cancellationToken);

        if (result == null)
        {
            return result;
        }

        result.TotalApprovalStages = result.ApprovalStages.Count;
        result.CompletedStages = result.ApprovalStages.Count(x => IsCompletedStatus(x.Status));
        var currentStage = result.ApprovalStages.OrderBy(x => x.StageOrder).FirstOrDefault(x => !IsCompletedStatus(x.Status));
        if (currentStage != null)
        {
            currentStage.IsCurrentStage = true;
        }
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
                        DocumentGuid = fv.DocumentGuid,
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
                application.ServiceId,
                ServiceName = application.Service != null ? application.Service.ServiceName : null,
                ServiceFees = application.Service != null ? application.Service.Fees : null,
                FeesRequired = application.Service != null && application.Service.FeesRequired
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (result == null)
        {
            return null;
        }

        // Application workflow is already completed.
        if (result.ApplicationStatus == ApplicationStatus.Approved ||
            result.ApplicationStatus == ApplicationStatus.Rejected)
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
                stage.UserId,
                stage.SLADays,
                stage.CanVerifyDocument,
                stage.CanApprove,
                stage.CanReject,
                stage.CanReturn,
                stage.CanPay,
                stage.CanEdit,
                stage.CanViewNoteSheet,
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
            .Where(user => user.Id == currentStage.UserId)
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
                $"Officer configured for stage '{currentStage.StageName}' was not found.");
        }

        var paymentTxn = await _paymentRepository.GetQueryable()
            .Include(p => p.PaymentStatus)
            .Where(p => p.ApplicationId == result.ApplicationId && p.PaymentStatus.StatusCode == "SUCCESS")
            .OrderByDescending(p => p.Id)
            .FirstOrDefaultAsync(cancellationToken);

        bool isPaid = paymentTxn != null;
        string? paymentStatus = paymentTxn != null ? "SUCCESS" : (result.FeesRequired && (result.ServiceFees ?? 0) > 0 ? "PENDING" : "NOT_REQUIRED");
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

            ServiceId = result.ServiceId,
            ServiceName = result.ServiceName,
            ServiceFees = result.ServiceFees,
            FeesRequired = result.FeesRequired,

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

        // Strict Statutory Government Fee Payment Verification Gate
        if (application.Service != null && application.Service.FeesRequired && (application.Service.Fees ?? 0) > 0)
        {
            var isPaid = await _paymentRepository.GetQueryable()
                .Include(p => p.PaymentStatus)
                .AnyAsync(p => p.ApplicationId == applicationId && p.PaymentStatus.StatusCode == "SUCCESS", cancellationToken);

            if (!isPaid)
            {
                throw new InvalidOperationException(
                    $"Cannot process application {application.ApplicationNo}. Government statutory fee of ₹{application.Service.Fees:F2} is pending. Payment must be recorded before proceeding.");
            }
        }

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
            .Select(stage => new { StageId = stage.Id, stage.StageOrder, stage.StageName, stage.UserId })
            .FirstOrDefaultAsync(cancellationToken);

        if (nextStage == null)
            throw new InvalidOperationException("Next approval stage is not configured.");

        //this Update ApplicationDetails Table
        application.CurrentApprovalFlowStageId = nextStage.StageId;
        application.CurrentStageOrder = nextStage.StageOrder;
        application.UserId = nextStage.UserId;
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
                name = "Citizen";
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

        // Strict Statutory Government Fee Payment Verification Gate
        if (application.Service != null && application.Service.FeesRequired && (application.Service.Fees ?? 0) > 0)
        {
            var isPaid = await _paymentRepository.GetQueryable()
                .Include(p => p.PaymentStatus)
                .AnyAsync(p => p.ApplicationId == applicationId && p.PaymentStatus.StatusCode == "SUCCESS", cancellationToken);

            if (!isPaid)
            {
                throw new InvalidOperationException(
                    $"Cannot approve application {application.ApplicationNo}. Government statutory fee of ₹{application.Service.Fees:F2} is pending. Payment must be recorded online or offline at municipal counter before approval.");
            }
        }

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

        //if Second Stage Is Last Satge
        if (currentStage.IsFinalStage)
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
            application.UpdatedBy = dto.UpdatedBy;
            application.UpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

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

        var nextStage = await _approvalFlowStageRepository  //PENDING AT
           .GetQueryable()
           .AsNoTracking()
           .Where(stage =>
               stage.ApprovalFlowId == application.ApprovalFlowId &&
               stage.StageOrder > application.CurrentStageOrder)
           .OrderBy(stage => stage.StageOrder)
           .Select(stage => new { StageId = stage.Id, stage.StageOrder, stage.StageName, stage.UserId })
           .FirstOrDefaultAsync(cancellationToken);

        if (nextStage == null)
            throw new InvalidOperationException("Next approval stage is not configured.");


        application.CurrentApprovalFlowStageId = nextStage.StageId;
        application.CurrentStageOrder = nextStage.StageOrder;
        application.UserId = nextStage.UserId;
        application.ApplicationStatus = ApplicationStatus.ApplicationVerified;
        application.Remark = dto.Remark;
        application.IsReverted = false;
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
                stage.StageOrder,
                stage.UserId
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

        if (!currentStage.CanEdit)
            throw new InvalidOperationException(
                $"{currentStage.StageName} does not permit application correction.");

        var fieldValues = await _fieldValueRepository
            .GetQueryable()
            .Where(x =>
                x.ApplicationId == applicationId &&
                x.IsActive &&
                !x.MarkedForDeletion)
            .ToListAsync(cancellationToken);

        if (!fieldValues.Any())
            throw new InvalidOperationException("Applicant field values were not found.");

        foreach (var item in dto.FieldValue)
        {
            var fieldValue = fieldValues.FirstOrDefault(x =>
                x.ApplicationId == applicationId &&
                x.FieldDefinitionId == item.FieldDefinitionId);

            if (fieldValue == null)
                throw new InvalidOperationException(
                    $"Field definition {item.FieldDefinitionId} was not found for this application.");

            fieldValue.TextValue = item.TextValue;
            fieldValue.NumberValue = item.NumberValue;
            fieldValue.DateValue = item.DateValue;
            fieldValue.BooleanValue = item.BooleanValue;
            fieldValue.DocumentGuid = item.DocumentGuid;
            fieldValue.UpdatedBy = dto.UpdatedBy;
            fieldValue.UpdatedDate = DateTime.Now;
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
            Status = ApplicationStatus.Correction,
            Action = $"{ApplicationStatus.Correction} at {currentStage.StageName}",
            Remark = dto.Remark,
            IsReverted = false,
            IsActive = true,
            CreatedBy = dto.UpdatedBy,
            CreatedDate = DateTime.Now
        };

        await _historyRepository.AddAsync(history, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
                stage.UserId,
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
                stage.StageName,
                stage.UserId
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (previousStage != null)
        {
            // Officer-wise revert: move application back to the previous stage/officer
            application.CurrentApprovalFlowStageId = previousStage.StageId;
            application.CurrentStageOrder = previousStage.StageOrder;
            application.UserId = previousStage.UserId;
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
            var fieldValues = await _fieldValueRepository.GetQueryable()
                .Include(f => f.FieldDefinition)
                .Where(f => f.ApplicationId == applicationId && !f.MarkedForDeletion && f.IsActive)
                .ToListAsync(ct);

            string? mobile = null;
            string? name = null;

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
                }
                else if (string.IsNullOrWhiteSpace(name) &&
                    (code.Contains("applicant") || code.Contains("name") ||
                     label.Contains("applicant") || label.Contains("name") || label.Contains("नाव")))
                {
                    name = val;
                }
            }

            var svc = await _serviceRepository.GetByIdAsync(serviceId, ct);
            var serviceName = svc?.ServiceName ?? "RTS Service";

            return (mobile, name ?? "Citizen", serviceName);
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
}
