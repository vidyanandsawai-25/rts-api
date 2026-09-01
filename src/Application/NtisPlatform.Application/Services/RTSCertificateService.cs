using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.Constants;
using NtisPlatform.Application.DTOs.RTSCertificate;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class RTSCertificateService : IRTSCertificateService
{
    private const int MaxDesignJsonLength = 5 * 1024 * 1024;

    private readonly IRepository<RTSServiceCertificateMasterEntity, int> _templateRepository;
    private readonly IRepository<RTSIssuedCertificateEntity, int> _issuedCertRepository;
    private readonly IRepository<RTSApplicationDetailsEntity, int> _applicationRepository;
    private readonly IRepository<RTSFieldValueEntity, int> _fieldValueRepository;
    private readonly IRepository<RTSServiceEntity, int> _serviceRepository;
    private readonly IRepository<RTSDepartmentEntity, int> _departmentRepository;
    private readonly IRepository<UserEntity, int> _userRepository;
    private readonly IRepository<ULBMasterEntity, int> _ulbRepository;
    private readonly IRepository<TrackApplicationHistoryEntity, int> _historyRepository;
    private readonly IRepository<UserRoleAllocationEntity, int> _userRoleAllocationRepository;
    private readonly IRepository<RTSApprovalFlowStageMasterEntity, int> _stageRepository;
    private readonly IRTSSmsNotificationService _smsNotificationService;
    private readonly IRTSDigitalSignatureService _digitalSignatureService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RTSCertificateService> _logger;

    public RTSCertificateService(
        IRepository<RTSServiceCertificateMasterEntity, int> templateRepository,
        IRepository<RTSIssuedCertificateEntity, int> issuedCertRepository,
        IRepository<RTSApplicationDetailsEntity, int> applicationRepository,
        IRepository<RTSFieldValueEntity, int> fieldValueRepository,
        IRepository<RTSServiceEntity, int> serviceRepository,
        IRepository<RTSDepartmentEntity, int> departmentRepository,
        IRepository<UserEntity, int> userRepository,
        IRepository<ULBMasterEntity, int> ulbRepository,
        IRepository<TrackApplicationHistoryEntity, int> historyRepository,
        IRepository<UserRoleAllocationEntity, int> userRoleAllocationRepository,
        IRepository<RTSApprovalFlowStageMasterEntity, int> stageRepository,
        IRTSSmsNotificationService smsNotificationService,
        IRTSDigitalSignatureService digitalSignatureService,
        IUnitOfWork unitOfWork,
        ILogger<RTSCertificateService> logger)
    {
        _templateRepository = templateRepository;
        _issuedCertRepository = issuedCertRepository;
        _applicationRepository = applicationRepository;
        _fieldValueRepository = fieldValueRepository;
        _serviceRepository = serviceRepository;
        _departmentRepository = departmentRepository;
        _userRepository = userRepository;
        _ulbRepository = ulbRepository;
        _historyRepository = historyRepository;
        _userRoleAllocationRepository = userRoleAllocationRepository;
        _stageRepository = stageRepository;
        _smsNotificationService = smsNotificationService;
        _digitalSignatureService = digitalSignatureService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<List<RTSCertificateTemplateDto>> GetAllTemplatesAsync(CancellationToken ct)
    {
        var list = await _templateRepository.GetQueryable()
            .AsNoTracking()
            .Include(t => t.Service)
            .Where(t => !t.MarkedForDeletion)
            .OrderByDescending(t => t.CreatedDate)
            .ToListAsync(ct);

        var departments = await _departmentRepository.GetQueryable()
            .AsNoTracking()
            .ToDictionaryAsync(d => d.Id, ct);

        return list.Select(t =>
        {
            var dto = MapToTemplateDto(t);
            if (t.Service != null && departments.TryGetValue(t.Service.DepartmentId, out var dept))
            {
                dto.DepartmentName = dept.DepartmentName;
            }
            return dto;
        }).ToList();
    }

    public async Task<RTSCertificateTemplateDto?> GetTemplateByIdAsync(int id, CancellationToken ct)
    {
        var entity = await _templateRepository.GetQueryable()
            .AsNoTracking()
            .Include(t => t.Service)
            .FirstOrDefaultAsync(t => t.Id == id && !t.MarkedForDeletion, ct);

        if (entity == null) return null;

        var dto = MapToTemplateDto(entity);
        if (entity.Service != null)
        {
            var dept = await _departmentRepository.GetByIdAsync(entity.Service.DepartmentId, ct);
            dto.DepartmentName = dept?.DepartmentName;
        }
        return dto;
    }

    public async Task<RTSCertificateTemplateDto?> GetTemplateByServiceIdAsync(int serviceId, CancellationToken ct)
    {
        var entity = await _templateRepository.GetQueryable()
            .AsNoTracking()
            .Include(t => t.Service)
            .FirstOrDefaultAsync(t => t.ServiceId == serviceId && t.IsActive && !t.MarkedForDeletion, ct);

        if (entity == null) return null;

        var dto = MapToTemplateDto(entity);
        if (entity.Service != null)
        {
            var dept = await _departmentRepository.GetByIdAsync(entity.Service.DepartmentId, ct);
            dto.DepartmentName = dept?.DepartmentName;
        }
        return dto;
    }

    public async Task<List<CertificateAvailableTagDto>> GetAvailableTagsForServiceAsync(int serviceId, CancellationToken ct)
    {
        var tags = new List<CertificateAvailableTagDto>
        {
            new() { TagKey = "{{ApplicationNo}}", TagLabelMarathi = "अर्ज क्रमांक", TagLabelEnglish = "Application Number", SourceType = "Citizen" },
            new() { TagKey = "{{ApplicantName}}", TagLabelMarathi = "अर्जदाराचे पूर्ण नाव", TagLabelEnglish = "Applicant Full Name", SourceType = "Citizen" },
            new() { TagKey = "{{ApplicantMobile}}", TagLabelMarathi = "अर्जदाराचा मोबाईल", TagLabelEnglish = "Applicant Mobile", SourceType = "Citizen" },
            new() { TagKey = "{{AppliedDate}}", TagLabelMarathi = "अर्जाचा दिनांक", TagLabelEnglish = "Applied Date", SourceType = "Citizen" },
            new() { TagKey = "{{currentData}}", TagLabelMarathi = "चालू दिनांक", TagLabelEnglish = "Current Date", SourceType = "System" },
            new() { TagKey = "{{currentDataMinusOne}}", TagLabelMarathi = "चालू दिनांकाच्या एक दिवस आधी", TagLabelEnglish = "Current Date Minus One Day", SourceType = "System" },
            new() { TagKey = "{{currentDataMinusTwo}}", TagLabelMarathi = "चालू दिनांकाच्या दोन दिवस आधी", TagLabelEnglish = "Current Date Minus Two Days", SourceType = "System" },
            new() { TagKey = "{{currentDataPlusOne}}", TagLabelMarathi = "चालू दिनांकानंतर एक दिवस", TagLabelEnglish = "Current Date Plus One Day", SourceType = "System" },
            new() { TagKey = "{{currentDataPlusTwo}}", TagLabelMarathi = "चालू दिनांकानंतर दोन दिवस", TagLabelEnglish = "Current Date Plus Two Days", SourceType = "System" },
            new() { TagKey = "{{ServiceName}}", TagLabelMarathi = "सेवेचे नाव", TagLabelEnglish = "Service Name", SourceType = "System" },
            new() { TagKey = "{{ServiceNameMarathi}}", TagLabelMarathi = "सेवेचे स्थानिक नाव", TagLabelEnglish = "Service Local Name", SourceType = "System" },
            new() { TagKey = "{{DepartmentName}}", TagLabelMarathi = "विभागाचे नाव", TagLabelEnglish = "Department Name", SourceType = "System" },
            new() { TagKey = "{{ULBName}}", TagLabelMarathi = "महानगरपालिकेचे नाव", TagLabelEnglish = "ULB Name", SourceType = "System" },
            new() { TagKey = "{{ULBNameMarathi}}", TagLabelMarathi = "महानगरपालिका स्थानिक नाव", TagLabelEnglish = "ULB Marathi Name", SourceType = "System" },
            new() { TagKey = "{{IssueDate}}", TagLabelMarathi = "प्रमाणपत्र जारी दिनांक", TagLabelEnglish = "Issue Date", SourceType = "System" },
            new() { TagKey = "{{CertificateNo}}", TagLabelMarathi = "प्रमाणपत्र / दाखला क्रमांक", TagLabelEnglish = "Certificate Number", SourceType = "System" },
            new() { TagKey = "{{ApprovedByOfficer}}", TagLabelMarathi = "मंजुरी अधिकाऱ्याचे नाव", TagLabelEnglish = "Approving Officer Name", SourceType = "System" },
            new() { TagKey = "{{OfficerDesignation}}", TagLabelMarathi = "अधिकाऱ्याचे पदनाम", TagLabelEnglish = "Officer Designation", SourceType = "System" },
            new() { TagKey = "{{OfficerRemark}}", TagLabelMarathi = "अधिकाऱ्याचा शेरा", TagLabelEnglish = "Officer Remark", SourceType = "Officer" },
            new() { TagKey = "{{QRCode}}", TagLabelMarathi = "सत्यता पडताळणी QR कोड", TagLabelEnglish = "Verification QR Code", SourceType = "System" },
            new() { TagKey = "{{DigitalSignature}}", TagLabelMarathi = "डिजिटल स्वाक्षरी सील", TagLabelEnglish = "Digital Signature Seal", SourceType = "System" },
            new() { TagKey = "[[OrderNo]]", TagLabelMarathi = "जावक / आदेश क्रमांक (अधिकाऱ्याने भरावयाचा)", TagLabelEnglish = "Outward/Order No (Officer Input)", SourceType = "Officer" },
            new() { TagKey = "[[ValidityPeriod]]", TagLabelMarathi = "परवाना वैधता मुदत (अधिकाऱ्याने भरावयाचा)", TagLabelEnglish = "Validity Period (Officer Input)", SourceType = "Officer" },
            new() { TagKey = "[[ChallanNo]]", TagLabelMarathi = "शुल्क चलन / पावती क्र. (अधिकाऱ्याने भरावयाचा)", TagLabelEnglish = "Receipt/Challan No (Officer Input)", SourceType = "Officer" },
            new() { TagKey = "[[SpecialConditions]]", TagLabelMarathi = "विशेष अटी व शर्ती (अधिकाऱ्याने भरावयाच्या)", TagLabelEnglish = "Terms & Conditions (Officer Input)", SourceType = "Officer" },
        };

        // Fetch dynamic form fields for this service
        var dynamicFields = await _fieldValueRepository.GetQueryable()
            .AsNoTracking()
            .Include(f => f.FieldDefinition)
            .Where(f => f.Application != null && f.Application.ServiceId == serviceId && !f.MarkedForDeletion && f.FieldDefinition != null)
            .Select(f => new { f.FieldDefinition!.FieldCode, f.FieldDefinition.FieldLabel, f.FieldDefinition.FieldLabelLocal })
            .Distinct()
            .Take(30)
            .ToListAsync(ct);

        foreach (var df in dynamicFields)
        {
            if (!string.IsNullOrWhiteSpace(df.FieldCode))
            {
                tags.Add(new CertificateAvailableTagDto
                {
                    TagKey = $"{{{{Field:{df.FieldCode}}}}}",
                    TagLabelMarathi = df.FieldLabelLocal ?? df.FieldLabel ?? df.FieldCode,
                    TagLabelEnglish = df.FieldLabel ?? df.FieldCode,
                    SourceType = "Citizen"
                });
            }
        }

        return tags;
    }

    public async Task<RTSCertificateTemplateDto> CreateTemplateAsync(CreateRTSCertificateTemplateDto dto, int userId, CancellationToken ct)
    {
        ValidateDesignJson(dto.DesignJson);

        var existing = await _templateRepository.GetQueryable()
            .FirstOrDefaultAsync(t => t.ServiceId == dto.ServiceId && !t.MarkedForDeletion, ct);

        if (existing != null)
        {
            throw new InvalidOperationException($"A certificate template already exists for ServiceId {dto.ServiceId}. Please update the existing template.");
        }

        var entity = new RTSServiceCertificateMasterEntity
        {
            ServiceId = dto.ServiceId,
            TemplateName = dto.TemplateName.Trim(),
            TemplateCode = string.IsNullOrWhiteSpace(dto.TemplateCode) ? $"CERT_{dto.ServiceId}_{DateTime.UtcNow.Ticks % 10000}" : dto.TemplateCode.Trim().ToUpper(),
            HeaderContent = dto.HeaderContent,
            BodyContent = dto.BodyContent,
            FooterContent = dto.FooterContent,
            DesignJson = dto.DesignJson,
            DefaultConditionsJson = dto.DefaultConditionsJson,
            OfficerFieldsConfigJson = dto.OfficerFieldsConfigJson,
            IsActive = dto.IsActive,
            CreatedBy = userId,
            CreatedDate = DateTime.UtcNow
        };

        await _templateRepository.AddAsync(entity, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return await GetTemplateByIdAsync(entity.Id, ct) ?? MapToTemplateDto(entity);
    }

    public async Task<RTSCertificateTemplateDto> UpdateTemplateAsync(UpdateRTSCertificateTemplateDto dto, int userId, CancellationToken ct)
    {
        if (dto.DesignJsonSpecified)
        {
            ValidateDesignJson(dto.DesignJson);
        }

        var entity = await _templateRepository.GetQueryable()
            .FirstOrDefaultAsync(t => t.Id == dto.Id && !t.MarkedForDeletion, ct);

        if (entity == null)
            throw new KeyNotFoundException($"Certificate template with ID {dto.Id} not found.");

        entity.ServiceId = dto.ServiceId;
        entity.TemplateName = dto.TemplateName.Trim();
        entity.TemplateCode = dto.TemplateCode?.Trim().ToUpper() ?? entity.TemplateCode;
        entity.HeaderContent = dto.HeaderContent;
        entity.BodyContent = dto.BodyContent;
        entity.FooterContent = dto.FooterContent;
        if (dto.DesignJsonSpecified)
        {
            entity.DesignJson = dto.DesignJson;
        }
        entity.DefaultConditionsJson = dto.DefaultConditionsJson;
        entity.OfficerFieldsConfigJson = dto.OfficerFieldsConfigJson;
        entity.IsActive = dto.IsActive;
        entity.UpdatedBy = userId;
        entity.UpdatedDate = DateTime.UtcNow;

        await _templateRepository.UpdateAsync(entity, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return await GetTemplateByIdAsync(entity.Id, ct) ?? MapToTemplateDto(entity);
    }

    public async Task<bool> DeleteTemplateAsync(int id, int userId, CancellationToken ct)
    {
        var entity = await _templateRepository.GetQueryable()
            .FirstOrDefaultAsync(t => t.Id == id && !t.MarkedForDeletion, ct);

        if (entity == null) return false;

        entity.MarkedForDeletion = true;
        entity.MarkedForDeletionDate = DateTime.UtcNow;
        entity.UpdatedBy = userId;
        entity.UpdatedDate = DateTime.UtcNow;

        await _templateRepository.UpdateAsync(entity, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return true;
    }

    public async Task<CertificatePreviewResponseDto> PreviewCertificateAsync(CertificatePreviewRequestDto request, CancellationToken ct)
    {
        var app = await _applicationRepository.GetQueryable()
            .AsNoTracking()
            .Include(a => a.Service)
            .Include(a => a.Department)
            .Include(a => a.FieldValueData)
                .ThenInclude(fv => fv.FieldDefinition)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId && !a.MarkedForDeletion, ct);

        if (app == null)
            throw new KeyNotFoundException($"Application with ID {request.ApplicationId} not found.");

        var template = await _templateRepository.GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.ServiceId == app.ServiceId && t.IsActive && !t.MarkedForDeletion, ct);

        if (app.Service != null && !app.Service.IsCertificateRequired)
        {
            return new CertificatePreviewResponseDto
            {
                HasTemplate = false,
                TemplateId = 0,
                TemplateName = "No Certificate Required",
                SampleCertificateNo = string.Empty,
                MergedHtml = "<div class='p-4 text-center text-slate-500 font-semibold'>या सेवेसाठी कोणतेही प्रमाणपत्र जारी करण्याची आवश्यकता नाही (IsCertificateRequired=false).</div>"
            };
        }

        string deptCode = !string.IsNullOrWhiteSpace(app.Department?.DepartmentCode)
            ? app.Department.DepartmentCode.Trim()
            : (!string.IsNullOrWhiteSpace(app.Service?.Department?.DepartmentCode)
                ? app.Service.Department.DepartmentCode.Trim()
                : (!string.IsNullOrWhiteSpace(app.Department?.DepartmentName) && app.Department.DepartmentName.Length >= 2
                    ? app.Department.DepartmentName[..Math.Min(3, app.Department.DepartmentName.Length)].ToUpperInvariant()
                    : "SRV"));

        var response = new CertificatePreviewResponseDto
        {
            HasTemplate = template != null,
            TemplateId = template?.Id ?? 0,
            TemplateName = template?.TemplateName ?? "Default Certificate Template",
            SampleCertificateNo = $"CERT/{deptCode}/{DateTime.UtcNow:yyyy}/{app.Id:D6}"
        };

        if (template != null)
        {
            var templateDto = MapToTemplateDto(template);
            response.RequiredOfficerFields = templateDto.OfficerFields;
            response.DefaultConditions = templateDto.DefaultConditions;
        }

        // Build Citizen Auto Values dictionary
        var autoValues = await BuildAutoValuesDictionaryAsync(app, ct);
        response.CitizenAutoValues = autoValues;

        // Perform merge
        string rawHtml = BuildFullCertificateHtml(template, app);
        string previewOfficerName = !string.IsNullOrWhiteSpace(app.User?.FirstName)
            ? $"{app.User.FirstName} {app.User.LastName}".Trim()
            : "सक्षम प्राधिकारी";

        response.MergedHtml = MergeTemplatePlaceholders(rawHtml, autoValues, request.OfficerInputs, request.CustomConditions, response.SampleCertificateNo, previewOfficerName, isLiveSigned: false);

        return response;
    }

    public async Task<RTSIssuedCertificateDto> IssueCertificateAsync(IssueCertificateRequestDto request, int userId, CancellationToken ct)
    {
        var app = await _applicationRepository.GetQueryable()
            .Include(a => a.Service)
            .Include(a => a.Department)
            .Include(a => a.FieldValueData)
                .ThenInclude(fv => fv.FieldDefinition)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId && !a.MarkedForDeletion, ct);

        if (app == null)
            throw new KeyNotFoundException($"Application with ID {request.ApplicationId} not found.");

        if (app.Service != null && !app.Service.IsCertificateRequired)
        {
            throw new InvalidOperationException($"Service '{app.Service.ServiceName}' is configured with IsCertificateRequired=false (No certificate issuance required).");
        }

        var user = await _userRepository.GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        var template = await _templateRepository.GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.ServiceId == app.ServiceId && t.IsActive && !t.MarkedForDeletion, ct);

        // Generate 100% Dynamic Certificate Department Code from DepartmentMaster.DepartmentCode
        string deptCode = !string.IsNullOrWhiteSpace(app.Department?.DepartmentCode)
            ? app.Department.DepartmentCode.Trim()
            : (!string.IsNullOrWhiteSpace(app.Service?.Department?.DepartmentCode)
                ? app.Service.Department.DepartmentCode.Trim()
                : (!string.IsNullOrWhiteSpace(app.Department?.DepartmentName) && app.Department.DepartmentName.Length >= 2
                    ? app.Department.DepartmentName[..Math.Min(3, app.Department.DepartmentName.Length)].ToUpperInvariant()
                    : "SRV"));

        // Check if certificate already exists for this application
        var existingCert = await _issuedCertRepository.GetQueryable()
            .FirstOrDefaultAsync(c => c.ApplicationId == app.Id && c.IsActive, ct);

        string certNo = existingCert?.CertificateNo ?? $"CERT/{deptCode}/{DateTime.UtcNow:yyyy}/{app.Id:D6}";
        var certGuid = existingCert?.CertificateGuid ?? Guid.NewGuid();
        int issuedCertId = existingCert?.Id ?? 0;

        var autoValues = await BuildAutoValuesDictionaryAsync(app, ct);
        string officerName = user != null ? $"{user.FirstName} {user.LastName}".Trim() : "सक्षम प्राधिकारी";
        string officerDesignation = autoValues.GetValueOrDefault("OfficerDesignation") ?? "";
        if (string.IsNullOrWhiteSpace(officerDesignation) || officerDesignation == "सक्षम प्राधिकारी")
        {
            if (user != null)
            {
                var userRole = await _userRoleAllocationRepository.GetQueryable()
                    .AsNoTracking()
                    .Include(r => r.UserRole)
                    .FirstOrDefaultAsync(r => r.UserId == user.Id && r.IsActive, ct);
                if (!string.IsNullOrWhiteSpace(userRole?.UserRole?.UserRoleName))
                {
                    officerDesignation = userRole.UserRole.UserRoleName;
                }
                else if (!string.IsNullOrWhiteSpace(app.Department?.DepartmentNameLocal))
                {
                    officerDesignation = $"{app.Department.DepartmentNameLocal} - सक्षम अधिकारी";
                }
                else if (!string.IsNullOrWhiteSpace(app.Department?.DepartmentName))
                {
                    officerDesignation = $"{app.Department.DepartmentName} - Competent Authority";
                }
            }
        }
        if (string.IsNullOrWhiteSpace(officerDesignation))
        {
            officerDesignation = "सक्षम प्राधिकारी";
        }

        string rawHtml = BuildFullCertificateHtml(template, app);

        var signatureResult = _digitalSignatureService.SignCertificate(certNo, officerName, officerDesignation, rawHtml);

        string mergedHtml = MergeTemplatePlaceholders(rawHtml, autoValues, request.OfficerInputs, request.CustomConditions, certNo, officerName, isLiveSigned: true, certGuid: certGuid);

        var verificationBaseUrl = await GetVerificationBaseUrlAsync(ct);
        string qrPayload = $"{verificationBaseUrl}/{certGuid}";

        if (existingCert != null)
        {
            existingCert.CertificateServiceId = template?.Id ?? 0;
            existingCert.OfficerInputsJson = request.OfficerInputs != null && request.OfficerInputs.Count > 0 ? JsonSerializer.Serialize(request.OfficerInputs) : null;
            existingCert.MergedHtmlContent = mergedHtml;
            existingCert.QrCodePayload = qrPayload;
            existingCert.IssuedByUserId = userId;
            existingCert.IssuedAt = DateTime.UtcNow;
            existingCert.IsDigitallySigned = true;
            existingCert.DigitalSignatureInfo = signatureResult.SignatureInfo;
            existingCert.UpdatedBy = userId;
            existingCert.UpdatedDate = DateTime.UtcNow;

            await _issuedCertRepository.UpdateAsync(existingCert, ct);
        }
        else
        {
            var issuedCert = new RTSIssuedCertificateEntity
            {
                CertificateGuid = certGuid,
                CertificateNo = certNo,
                ApplicationId = app.Id,
                ServiceId = app.ServiceId,
                CertificateServiceId = template?.Id ?? 0,
                OfficerInputsJson = request.OfficerInputs != null && request.OfficerInputs.Count > 0 ? JsonSerializer.Serialize(request.OfficerInputs) : null,
                MergedHtmlContent = mergedHtml,
                QrCodePayload = qrPayload,
                IssuedByUserId = userId,
                IssuedAt = DateTime.UtcNow,
                IsDigitallySigned = true,
                DigitalSignatureInfo = signatureResult.SignatureInfo,
                IsActive = true,
                CreatedBy = userId,
                CreatedDate = DateTime.UtcNow
            };

            await _issuedCertRepository.AddAsync(issuedCert, ct);
            issuedCertId = issuedCert.Id;
        }

        // Immutable ERP Audit Trail
        var auditHistory = new TrackApplicationHistoryEntity
        {
            ApplicationId = app.Id,
            ApprovalFlowId = app.ApprovalFlowId,
            ApprovalFlowStageId = app.CurrentApprovalFlowStageId,
            ActionByUserId = userId,
            Status = ApplicationStatus.Approved,
            Action = "IssueCertificateAndDigitalSign",
            Remark = !string.IsNullOrWhiteSpace(request.ActionRemark)
                ? request.ActionRemark
                : $"प्रमाणपत्र क्र. {certNo} डिजिटल स्वाक्षरीने अधिकृतरीत्या जारी केले. (DSC Hash: {signatureResult.SignatureHash[..Math.Min(16, signatureResult.SignatureHash.Length)]}...)",
            IsReverted = false,
            CreatedBy = userId,
            CreatedDate = DateTime.UtcNow
        };
        await _historyRepository.AddAsync(auditHistory, ct);

        // If SignAndApprove is checked, complete the application workflow approval
        if (request.SignAndApprove)
        {
            if (app.ApplicationStatus != ApplicationStatus.Approved)
            {
                app.ApplicationStatus = ApplicationStatus.Approved;
            }
            if (!string.IsNullOrWhiteSpace(request.ActionRemark))
            {
                app.Remark = request.ActionRemark;
            }
            app.UpdatedBy = userId;
            app.UpdatedDate = DateTime.UtcNow;
            await _applicationRepository.UpdateAsync(app, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        // Dispatch unified DLT approval SMS notification with certificate tracking link
        try
        {
            var mobile = app.ApplicantMobileNo;
            if (string.IsNullOrWhiteSpace(mobile))
            {
                var mobileFv = await _fieldValueRepository.GetQueryable()
                    .Include(f => f.FieldDefinition)
                    .Where(f => f.ApplicationId == app.Id && !f.MarkedForDeletion && f.IsActive)
                    .FirstOrDefaultAsync(f => f.FieldDefinition != null && (
                        f.FieldDefinition.FieldCode.ToLower().Contains("mobile") ||
                        f.FieldDefinition.FieldLabel.ToLower().Contains("mobile") ||
                        f.FieldDefinition.FieldLabel.Contains("मोबाईल")
                    ), ct);
                mobile = mobileFv?.TextValue?.Trim();
            }

            var serviceName = app.Service?.ServiceName ?? "RTS Service";
            var appNo = app.ApplicationNo ?? $"RTS{app.Id:D8}";

            if (!string.IsNullOrWhiteSpace(mobile))
            {
                await _smsNotificationService.SendApplicationCertificateIssuedAsync(
                    app.Id,
                    appNo,
                    app.ApplicantName ?? "नागरिक",
                    mobile,
                    serviceName,
                    ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to dispatch certificate approval SMS to applicant.");
        }

        return await GetIssuedCertificateByGuidAsync(certGuid, ct)
            ?? new RTSIssuedCertificateDto
            {
                CertificateGuid = certGuid,
                CertificateNo = certNo,
                ApplicationId = app.Id,
                ApplicationNo = app.ApplicationNo ?? $"RTS{app.Id:D8}",
                ServiceName = app.Service?.ServiceName ?? "",
                ApplicantName = app.ApplicantName ?? "",
                OfficerInputs = request.OfficerInputs ?? new Dictionary<string, string>(),
                MergedHtmlContent = mergedHtml,
                QrCodePayload = qrPayload,
                IssuedByUserId = userId,
                IssuedAt = DateTime.UtcNow,
                IsDigitallySigned = true,
                DigitalSignatureInfo = signatureResult.SignatureInfo
            };
    }

    public async Task<RTSIssuedCertificateDto?> GetIssuedCertificateByApplicationNoAsync(string applicationNo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(applicationNo)) return null;

        string cleanNo = applicationNo.Trim();
        int parsedId = 0;
        if (int.TryParse(cleanNo, out int directId))
        {
            parsedId = directId;
        }
        else if (cleanNo.StartsWith("RTS", StringComparison.OrdinalIgnoreCase))
        {
            int.TryParse(cleanNo.Substring(3), out parsedId);
        }

        string paddedAppNo = parsedId > 0 ? $"RTS{parsedId:D8}" : cleanNo;

        var cert = await _issuedCertRepository.GetQueryable()
            .AsNoTracking()
            .Include(c => c.Application)
            .Include(c => c.Service)
            .Include(c => c.IssuedByUser)
            .Where(c => !c.MarkedForDeletion && (
                (c.Application != null && c.Application.ApplicationNo == cleanNo) ||
                (c.Application != null && c.Application.ApplicationNo == paddedAppNo) ||
                (parsedId > 0 && c.ApplicationId == parsedId) ||
                c.CertificateNo == cleanNo
            ))
            .OrderByDescending(c => c.IssuedAt)
            .FirstOrDefaultAsync(ct);

        if (cert == null) return null;

        var dto = MapToIssuedCertDto(cert);
        if (cert.Service != null)
        {
            var dept = await _departmentRepository.GetByIdAsync(cert.Service.DepartmentId, ct);
            dto.DepartmentName = dept?.DepartmentName ?? "";
        }
        return dto;
    }

    public async Task<RTSIssuedCertificateDto?> GetIssuedCertificateByGuidAsync(Guid certificateGuid, CancellationToken ct)
    {
        var cert = await _issuedCertRepository.GetQueryable()
            .AsNoTracking()
            .Include(c => c.Application)
            .Include(c => c.Service)
            .Include(c => c.IssuedByUser)
            .FirstOrDefaultAsync(c => c.CertificateGuid == certificateGuid && !c.MarkedForDeletion, ct);

        if (cert == null) return null;

        var dto = MapToIssuedCertDto(cert);
        if (cert.Service != null)
        {
            var dept = await _departmentRepository.GetByIdAsync(cert.Service.DepartmentId, ct);
            dto.DepartmentName = dept?.DepartmentName ?? "";
        }
        return dto;
    }

    public async Task<CertificateVerificationResponseDto> VerifyCertificatePublicAsync(Guid certificateGuid, CancellationToken ct)
    {
        var cert = await _issuedCertRepository.GetQueryable()
            .AsNoTracking()
            .Include(c => c.Application)
            .Include(c => c.Service)
            .Include(c => c.IssuedByUser)
            .FirstOrDefaultAsync(c => c.CertificateGuid == certificateGuid && !c.MarkedForDeletion, ct);

        if (cert == null)
        {
            return new CertificateVerificationResponseDto
            {
                IsValid = false,
                CertificateGuid = certificateGuid,
                Message = "सदर क्यूआर कोड किंवा प्रमाणपत्र क्रमांक अवैध आहे. (Invalid or Unverified Certificate)"
            };
        }

        var ulb = await _ulbRepository.GetQueryable().AsNoTracking().FirstOrDefaultAsync(ct);
        string ulbName = ulb?.UlbNameLocal ?? ulb?.UlbName ?? "नागरी स्थानिक संस्था (ULB)";

        string deptName = "";
        if (cert.Service != null)
        {
            var dept = await _departmentRepository.GetByIdAsync(cert.Service.DepartmentId, ct);
            deptName = dept?.DepartmentNameLocal ?? dept?.DepartmentName ?? "";
        }

        string officerName = cert.IssuedByUser != null ? $"{cert.IssuedByUser.FirstName} {cert.IssuedByUser.LastName}".Trim() : "सक्षम प्राधिकारी";
        string officerDesignation = "सक्षम प्राधिकारी";

        if (cert.IssuedByUser != null)
        {
            var userRole = await _userRoleAllocationRepository.GetQueryable()
                .AsNoTracking()
                .Include(r => r.UserRole)
                .FirstOrDefaultAsync(r => r.UserId == cert.IssuedByUser.Id && r.IsActive, ct);

            if (!string.IsNullOrWhiteSpace(userRole?.UserRole?.UserRoleName))
            {
                officerDesignation = userRole.UserRole.UserRoleName;
            }
            else if (!string.IsNullOrWhiteSpace(deptName))
            {
                officerDesignation = $"{deptName} - सक्षम अधिकारी";
            }
        }

        var dscMetadata = _digitalSignatureService.GetCertificateMetadata();

        return new CertificateVerificationResponseDto
        {
            IsValid = true,
            CertificateGuid = cert.CertificateGuid,
            CertificateNo = cert.CertificateNo,
            ApplicationNo = cert.Application?.ApplicationNo ?? $"RTS{cert.ApplicationId:D8}",
            ServiceName = cert.Service?.ServiceNameLocal ?? cert.Service?.ServiceName ?? "",
            DepartmentName = deptName,
            ApplicantName = cert.Application?.ApplicantName ?? "",
            UlbName = ulbName,
            IssuedAt = cert.IssuedAt,
            IssuedByOfficer = officerName,
            OfficerDesignation = officerDesignation,
            IsDigitallySigned = cert.IsDigitallySigned,
            DigitalSignatureInfo = cert.DigitalSignatureInfo,
            DscSignerName = dscMetadata?.SignerName ?? "Authorized Document Signer",
            DscIssuer = dscMetadata?.Issuer ?? "Certifying Authority",
            DscSerialNumber = dscMetadata?.SerialNumber ?? "",
            DscThumbprint = dscMetadata?.Thumbprint ?? "",
            DscValidUntil = dscMetadata?.ValidTo,
            MergedHtmlContent = cert.MergedHtmlContent,
            Message = "✅ हे प्रमाणपत्र अधिकृतरीत्या पडताळलेले व अस्सल आहे. (Officially Verified & Authentic Certificate)"
        };
    }

    private async Task<string> GetVerificationBaseUrlAsync(CancellationToken ct)
    {
        try
        {
            var ulb = await _ulbRepository.GetQueryable().FirstOrDefaultAsync(u => u.IsActive, ct);
            if (!string.IsNullOrWhiteSpace(ulb?.WebsiteUrl))
            {
                var cleanUrl = ulb.WebsiteUrl.Trim().TrimEnd('/');
                if (!cleanUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !cleanUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    cleanUrl = $"https://{cleanUrl}";
                }
                if (cleanUrl.EndsWith("/service", StringComparison.OrdinalIgnoreCase))
                {
                    cleanUrl = cleanUrl[..^8].TrimEnd('/');
                }
                return $"{cleanUrl}/service/verify-certificate";
            }
        }
        catch { }

        return "/service/verify-certificate";
    }

    // Helper Methods
    private async Task<Dictionary<string, string>> BuildAutoValuesDictionaryAsync(RTSApplicationDetailsEntity app, CancellationToken ct)
    {
        var ulb = await _ulbRepository.GetQueryable().AsNoTracking().FirstOrDefaultAsync(ct);

        var currentDate = DateTime.UtcNow.Date;
        string appDate = (app.CreatedDate ?? currentDate).ToString("dd/MM/yyyy");
        string issueDate = currentDate.ToString("dd/MM/yyyy");
        string srvNameMarathi = app.Service?.ServiceNameLocal ?? app.Service?.ServiceName ?? "लोकसेवा";
        string srvNameEng = app.Service?.ServiceName ?? "RTS Service";
        string deptNameMarathi = app.Department?.DepartmentNameLocal ?? app.Department?.DepartmentName ?? "महानगरपालिका विभाग";
        string ulbNameMarathi = ulb?.UlbNameLocal ?? ulb?.UlbName ?? "महानगरपालिका";
        string ulbNameEng = ulb?.UlbName ?? "Municipal Corporation";
        string ulbAddress = !string.IsNullOrWhiteSpace(ulb?.UlbAddress) ? ulb.UlbAddress : "महानगरपालिका मुख्य कार्यालय";
        string ulbMobile = !string.IsNullOrWhiteSpace(ulb?.MobileNo) ? ulb.MobileNo : "-";
        string ulbEmail = !string.IsNullOrWhiteSpace(ulb?.EmailId) ? ulb.EmailId : "-";
        string ulbWebsite = !string.IsNullOrWhiteSpace(ulb?.WebsiteUrl) ? ulb.WebsiteUrl : "-";

        string ulbCode = !string.IsNullOrWhiteSpace(ulb?.UlbCode) ? ulb.UlbCode : "RTS";
        string ulbShortCode = !string.IsNullOrWhiteSpace(ulb?.UlbNameLocal) ? ulb.UlbNameLocal.Split(' ').FirstOrDefault() ?? "मनपा" : "मनपा";
        string currentYear = DateTime.UtcNow.Year.ToString();

        string deptCode = !string.IsNullOrWhiteSpace(app.Department?.DepartmentCode)
            ? app.Department.DepartmentCode.Trim()
            : (!string.IsNullOrWhiteSpace(app.Service?.Department?.DepartmentCode)
                ? app.Service.Department.DepartmentCode.Trim()
                : (!string.IsNullOrWhiteSpace(app.Department?.DepartmentName) && app.Department.DepartmentName.Length >= 2
                    ? app.Department.DepartmentName[..Math.Min(3, app.Department.DepartmentName.Length)].ToUpperInvariant()
                    : "SRV"));

        string serviceCode = !string.IsNullOrWhiteSpace(app.Service?.ServiceCode)
            ? app.Service.ServiceCode.Trim()
            : $"SRV{app.ServiceId:D3}";

        string standardOutwardNo = $"{ulbShortCode}/{deptCode}/{currentYear}/{app.Id:D6}";

        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ApplicationNo"] = app.ApplicationNo ?? $"RTS{app.Id:D8}",
            ["ApplicantName"] = app.ApplicantName ?? "सन्माननीय नागरिक",
            ["ApplicantMobile"] = app.ApplicantMobileNo ?? "-",
            ["ApplicantAddress"] = !string.IsNullOrWhiteSpace(ulb?.District) ? $"{ulb.District}, महाराष्ट्र" : "महाराष्ट्र",
            ["AppliedDate"] = appDate,
            ["ApplicationDate"] = appDate,
            ["ApprovalDate"] = issueDate,
            ["IssueDate"] = issueDate,
            ["currentData"] = currentDate.ToString("dd/MM/yyyy"),
            ["currentDataMinusOne"] = currentDate.AddDays(-1).ToString("dd/MM/yyyy"),
            ["currentDataMinusTwo"] = currentDate.AddDays(-2).ToString("dd/MM/yyyy"),
            ["currentDataPlusOne"] = currentDate.AddDays(1).ToString("dd/MM/yyyy"),
            ["currentDataPlusTwo"] = currentDate.AddDays(2).ToString("dd/MM/yyyy"),
            ["ServiceTitle"] = srvNameMarathi,
            ["ServiceName"] = srvNameEng,
            ["ServiceNameMarathi"] = srvNameMarathi,
            ["ServiceCode"] = serviceCode,
            ["DepartmentName"] = deptNameMarathi,
            ["DepartmentNameMarathi"] = deptNameMarathi,
            ["DepartmentNameEnglish"] = app.Department?.DepartmentName ?? "",
            ["DepartmentCode"] = deptCode,
            ["OutwardNo"] = standardOutwardNo,
            ["OrderNo"] = standardOutwardNo,
            ["ULBCode"] = ulbCode,
            ["ULBShortCode"] = ulbShortCode,
            ["Year"] = currentYear,
            ["YearMarathi"] = currentYear,
            ["ULBName"] = ulbNameMarathi,
            ["ULBNameMarathi"] = ulbNameMarathi,
            ["ULBNameEnglish"] = ulbNameEng,
            ["ULBAddress"] = ulbAddress,
            ["ULBMobile"] = ulbMobile,
            ["ULBEmail"] = ulbEmail,
            ["ULBWebsite"] = ulbWebsite
        };

        // Extract values from dynamic FieldValueData
        if (app.FieldValueData != null)
        {
            foreach (var fv in app.FieldValueData)
            {
                if (fv.FieldDefinition != null && !string.IsNullOrWhiteSpace(fv.FieldDefinition.FieldCode))
                {
                    string val = fv.TextValue ?? fv.NumberValue?.ToString() ?? fv.DateValue?.ToString("dd/MM/yyyy") ?? (fv.BooleanValue.HasValue ? (fv.BooleanValue.Value ? "होय" : "नाही") : "");
                    string code = fv.FieldDefinition.FieldCode;

                    dict[$"Field:{code}"] = val;
                    dict[code] = val;

                    // Common synonyms
                    if (code.Contains("Address", StringComparison.OrdinalIgnoreCase) || code.Contains("Patt", StringComparison.OrdinalIgnoreCase) || code.Contains("Addr", StringComparison.OrdinalIgnoreCase))
                    {
                        dict["ApplicantAddress"] = val;
                        dict["Address"] = val;
                    }
                    if (code.Contains("Upic", StringComparison.OrdinalIgnoreCase))
                        dict["UpicNo"] = val;
                    if (code.Contains("PropertyNo", StringComparison.OrdinalIgnoreCase))
                        dict["PropertyNo"] = val;
                    if (code.Contains("Ward", StringComparison.OrdinalIgnoreCase))
                        dict["WardName"] = val;
                    if (code.Contains("Zone", StringComparison.OrdinalIgnoreCase))
                    {
                        dict["ZoneName"] = val;
                        dict["ZoneType"] = val;
                    }
                    if (code.Contains("Mobile", StringComparison.OrdinalIgnoreCase) || code.Contains("Phone", StringComparison.OrdinalIgnoreCase))
                        dict["ApplicantMobile"] = val;
                    if (code.Contains("Mouja", StringComparison.OrdinalIgnoreCase) || code.Contains("Village", StringComparison.OrdinalIgnoreCase) || code.Contains("गाव", StringComparison.OrdinalIgnoreCase) || code.Contains("मौजे", StringComparison.OrdinalIgnoreCase))
                        dict["MoujaName"] = val;
                    if (code.Contains("Taluka", StringComparison.OrdinalIgnoreCase) || code.Contains("तालुका", StringComparison.OrdinalIgnoreCase))
                        dict["TalukaName"] = val;
                    if (code.Contains("Survey", StringComparison.OrdinalIgnoreCase) || code.Contains("Gat", StringComparison.OrdinalIgnoreCase) || code.Contains("Plot", StringComparison.OrdinalIgnoreCase) || code.Contains("CTS", StringComparison.OrdinalIgnoreCase) || code.Contains("गट", StringComparison.OrdinalIgnoreCase) || code.Contains("सर्व्हे", StringComparison.OrdinalIgnoreCase))
                    {
                        dict["SurveyPlotNo"] = val;
                        if (!dict.ContainsKey("PropertyNo") || dict["PropertyNo"] == "-") dict["PropertyNo"] = val;
                    }
                    if (code.Contains("Area", StringComparison.OrdinalIgnoreCase) || code.Contains("क्षेत्र", StringComparison.OrdinalIgnoreCase))
                        dict["LandArea"] = val;
                }
            }
        }

        return dict;
    }

    private string MergeTemplatePlaceholders(
        string rawTemplateHtml,
        Dictionary<string, string> citizenValues,
        Dictionary<string, string>? officerInputs,
        string? customConditions,
        string? sampleCertNo,
        string? officerName,
        bool isLiveSigned,
        Guid? certGuid = null)
    {
        string html = rawTemplateHtml;

        // 1. Replace Citizen dynamic fields {{TagName}}
        foreach (var (k, v) in citizenValues)
        {
            html = html.Replace($"{{{{{k}}}}}", v ?? "", StringComparison.OrdinalIgnoreCase);
        }

        // Common system tags
        html = html.Replace("{{CertificateNo}}", sampleCertNo ?? "", StringComparison.OrdinalIgnoreCase);
        html = html.Replace("{{ApprovedByOfficer}}", officerName ?? "", StringComparison.OrdinalIgnoreCase);
        html = html.Replace("{{OfficerDesignation}}", citizenValues.GetValueOrDefault("OfficerDesignation") ?? citizenValues.GetValueOrDefault("DepartmentName") ?? "", StringComparison.OrdinalIgnoreCase);
        html = html.Replace("{{ApprovalDate}}", DateTime.UtcNow.ToString("dd/MM/yyyy"), StringComparison.OrdinalIgnoreCase);

        var officerRemark = officerInputs?
            .FirstOrDefault(input => string.Equals(input.Key, "OfficerRemark", StringComparison.OrdinalIgnoreCase))
            .Value ?? string.Empty;
        var officerRemarkHtml = WebUtility.HtmlEncode(officerRemark)
            .Replace("\r\n", "<br />", StringComparison.Ordinal)
            .Replace("\n", "<br />", StringComparison.Ordinal);
        html = html.Replace("{{OfficerRemark}}", officerRemarkHtml, StringComparison.OrdinalIgnoreCase);
        html = html.Replace("[[OfficerRemark]]", officerRemarkHtml, StringComparison.OrdinalIgnoreCase);

        // 2. Replace Officer Inputs [[FieldKey]]
        if (officerInputs != null)
        {
            foreach (var (k, v) in officerInputs)
            {
                if (!string.IsNullOrWhiteSpace(v))
                {
                    html = html.Replace($"[[{k}]]", v, StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        // Automatic Dynamic Fallback for OutwardNo / OrderNo if left blank by officer
        string fallbackOutward = citizenValues.GetValueOrDefault("OutwardNo") ?? sampleCertNo ?? $"OUT/RTS/{DateTime.UtcNow:yyyy}";
        html = html.Replace("[[OutwardNo]]", fallbackOutward, StringComparison.OrdinalIgnoreCase);
        html = html.Replace("[[OrderNo]]", fallbackOutward, StringComparison.OrdinalIgnoreCase);
        html = html.Replace("{{OutwardNo}}", fallbackOutward, StringComparison.OrdinalIgnoreCase);
        html = html.Replace("{{OrderNo}}", fallbackOutward, StringComparison.OrdinalIgnoreCase);

        // Check if [[SpecialConditions]] exists; replace with customConditions if provided
        if (!string.IsNullOrWhiteSpace(customConditions))
        {
            if (html.Contains("[[SpecialConditions]]", StringComparison.OrdinalIgnoreCase))
            {
                html = html.Replace("[[SpecialConditions]]", customConditions, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                // If template does not have [[SpecialConditions]] tag, append custom conditions seamlessly before terms & conditions or signatures
                var conditionLines = customConditions.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var formattedConditions = string.Join("", conditionLines.Select(c => $"<li>{c.Trim()}</li>"));

                if (html.Contains("</ol>", StringComparison.OrdinalIgnoreCase))
                {
                    html = html.Replace("</ol>", $"{formattedConditions}</ol>", StringComparison.OrdinalIgnoreCase);
                }
                else if (html.Contains("</ul>", StringComparison.OrdinalIgnoreCase))
                {
                    html = html.Replace("</ul>", $"{formattedConditions}</ul>", StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    string extraConditionBox = $@"
                    <div class='extra-conditions-box my-2 p-2.5 bg-amber-50/60 border border-amber-300 rounded text-xs text-slate-800'>
                        <div class='font-bold text-amber-900 mb-1'>विशेष अटी व शर्ती (Special Conditions):</div>
                        <ul class='list-disc pl-5 space-y-0.5'>{formattedConditions}</ul>
                    </div>";

                    if (html.Contains("{{DigitalSignature}}", StringComparison.OrdinalIgnoreCase))
                    {
                        html = html.Replace("{{DigitalSignature}}", $"{extraConditionBox}\n{{{{DigitalSignature}}}}", StringComparison.OrdinalIgnoreCase);
                    }
                    else
                    {
                        html += extraConditionBox;
                    }
                }
            }
        }

        // Dynamically build and inject {{OfficerFieldsBlock}} if present or if officer inputs exist
        if (officerInputs != null && officerInputs.Count > 0)
        {
            var filledInputs = officerInputs
                .Where(kv => !string.Equals(kv.Key, "OfficerRemark", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(kv.Value))
                .ToList();
            if (filledInputs.Count > 0)
            {
                var officerBlockRows = string.Join("", filledInputs.Select(kv => $@"
                    <tr class='border-b border-slate-200'>
                        <td class='p-2 font-bold text-slate-700 w-1/3 bg-slate-50 border-r border-slate-200'>{kv.Key}:</td>
                        <td class='p-2 text-slate-900 font-semibold'>{kv.Value}</td>
                    </tr>"));

                string dynamicOfficerBlock = $@"
                <div class='officer-inputs-table my-3 border border-slate-300 rounded-lg overflow-hidden text-xs'>
                    <div class='bg-slate-100 p-2 font-bold text-slate-800 border-b border-slate-300 flex items-center gap-1.5'>
                        <span>📝</span> <span>अधिकारी निर्णय व तपासणी तपशील (Officer Inputs & Decision):</span>
                    </div>
                    <table class='w-full border-collapse'>{officerBlockRows}</table>
                </div>";

                if (html.Contains("{{OfficerFieldsBlock}}", StringComparison.OrdinalIgnoreCase))
                {
                    html = html.Replace("{{OfficerFieldsBlock}}", dynamicOfficerBlock, StringComparison.OrdinalIgnoreCase);
                }
                else if (html.Contains("{{DigitalSignature}}", StringComparison.OrdinalIgnoreCase))
                {
                    html = html.Replace("{{DigitalSignature}}", $"{dynamicOfficerBlock}\n{{{{DigitalSignature}}}}", StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    html += dynamicOfficerBlock;
                }
            }
        }

        // Clean any remaining unreplaced {{OfficerFieldsBlock}} or [[OfficerField]] tags
        html = html.Replace("{{OfficerFieldsBlock}}", "", StringComparison.OrdinalIgnoreCase);
        html = Regex.Replace(html, @"\[\[\w+\]\]", "");

        // 3. Inject Dynamic Official Seal Stamp, QR Code and Digital Signature Blocks
        string sealStampHtml = @"
        <div class='official-seal-stamp inline-block text-center'>
            <img src='/images/ulb-seal.png' alt='' class='w-28 h-28 object-contain transform -rotate-6 filter drop-shadow-xs inline-block' onerror=""this.style.display='none'"" />
        </div>";

        string qrCodeHtml = $@"
        <div class='inline-flex flex-col items-center justify-center p-2 bg-white border border-slate-300 rounded shadow-xs text-center'>
            <div class='w-20 h-20 bg-slate-100 flex items-center justify-center border border-slate-200 text-xs font-mono text-slate-700'>
                <svg class='w-16 h-16 text-slate-800' viewBox='0 0 24 24' fill='currentColor'>
                    <path d='M3 3h8v8H3V3zm2 2v4h4V5H5zm8-2h8v8h-8V3zm2 2v4h4V5h-4zM3 13h8v8H3v-8zm2 2v4h4v-4H5zm13-2h3v2h-3v-2zm-5 0h2v3h-2v-3zm2 3h3v2h-3v-2zm3 2h3v3h-3v-3zm-5 1h2v2h-2v-2zm2 0h1v2h-1v-2z'/>
                </svg>
            </div>
            <span class='text-[10px] text-slate-500 mt-1 font-semibold'>Scan to Verify</span>
        </div>";

        string officerDesignationDynamic = citizenValues.GetValueOrDefault("OfficerDesignation") ?? citizenValues.GetValueOrDefault("DepartmentName") ?? "";
        string signatureHtml = _digitalSignatureService.GenerateSignatureHtml(officerName, officerDesignationDynamic, DateTime.UtcNow, sampleCertNo ?? "");

        html = html.Replace("{{OfficialSealStamp}}", sealStampHtml, StringComparison.OrdinalIgnoreCase);
        html = html.Replace("{{QRCode}}", qrCodeHtml, StringComparison.OrdinalIgnoreCase);
        html = html.Replace("{{DigitalSignature}}", signatureHtml, StringComparison.OrdinalIgnoreCase);

        return html;
    }

    private string BuildFullCertificateHtml(RTSServiceCertificateMasterEntity? template, RTSApplicationDetailsEntity app)
    {
        // Dynamic service certificate loaded directly from RTS.ServiceCertificateMaster.
        if (template != null && !string.IsNullOrWhiteSpace(template.BodyContent))
        {
            // New canvas documents compile every page as a complete print surface and
            // repeat shared header/footer layers inside BodyContent. Keep the separate
            // fields for editing/reuse, but do not prepend/append them at runtime.
            if (template.BodyContent.Contains("data-certificate-multipage=\"true\"", StringComparison.OrdinalIgnoreCase))
            {
                return template.BodyContent;
            }

            if (!string.IsNullOrWhiteSpace(template.HeaderContent) && !string.IsNullOrWhiteSpace(template.FooterContent))
            {
                return $"{template.HeaderContent}\n{template.BodyContent}\n{template.FooterContent}";
            }
            return template.BodyContent;
        }

        return string.Empty;
    }

    private static void ValidateDesignJson(string? designJson)
    {
        if (designJson is null)
        {
            return;
        }

        if (designJson.Length > MaxDesignJsonLength)
        {
            throw new ArgumentException(
                $"DesignJson cannot exceed {MaxDesignJsonLength} characters.",
                nameof(designJson));
        }

        try
        {
            using var document = JsonDocument.Parse(designJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentException("DesignJson root value must be a JSON object.", nameof(designJson));
            }

        }
        catch (JsonException exception)
        {
            throw new ArgumentException("DesignJson must contain valid JSON.", nameof(designJson), exception);
        }
    }

    private static RTSCertificateTemplateDto MapToTemplateDto(RTSServiceCertificateMasterEntity entity)
    {
        var dto = new RTSCertificateTemplateDto
        {
            Id = entity.Id,
            ServiceId = entity.ServiceId,
            ServiceName = entity.Service?.ServiceNameLocal ?? entity.Service?.ServiceName ?? "",
            DepartmentName = "",
            TemplateName = entity.TemplateName,
            TemplateCode = entity.TemplateCode,
            HeaderContent = entity.HeaderContent,
            BodyContent = entity.BodyContent,
            FooterContent = entity.FooterContent,
            DesignJson = entity.DesignJson,
            DefaultConditionsJson = entity.DefaultConditionsJson,
            OfficerFieldsConfigJson = entity.OfficerFieldsConfigJson,
            IsActive = entity.IsActive,
            CreatedDate = entity.CreatedDate ?? DateTime.UtcNow,
            UpdatedDate = entity.UpdatedDate
        };

        if (!string.IsNullOrWhiteSpace(entity.OfficerFieldsConfigJson))
        {
            try
            {
                dto.OfficerFields = JsonSerializer.Deserialize<List<OfficerFieldConfigDto>>(entity.OfficerFieldsConfigJson) ?? new();
            }
            catch {}
        }

        if (!string.IsNullOrWhiteSpace(entity.DefaultConditionsJson))
        {
            try
            {
                dto.DefaultConditions = JsonSerializer.Deserialize<List<string>>(entity.DefaultConditionsJson) ?? new();
            }
            catch {}
        }

        return dto;
    }

    private static RTSIssuedCertificateDto MapToIssuedCertDto(RTSIssuedCertificateEntity cert)
    {
        return new RTSIssuedCertificateDto
        {
            Id = cert.Id,
            CertificateGuid = cert.CertificateGuid,
            CertificateNo = cert.CertificateNo,
            ApplicationId = cert.ApplicationId,
            ApplicationNo = cert.Application?.ApplicationNo ?? $"RTS{cert.ApplicationId:D8}",
            ServiceId = cert.ServiceId,
            ServiceName = cert.Service?.ServiceNameLocal ?? cert.Service?.ServiceName ?? "",
            DepartmentName = cert.Application?.Department?.DepartmentNameLocal ?? cert.Application?.Department?.DepartmentName ?? "",
            ApplicantName = cert.Application?.ApplicantName ?? "",
            ApplicantMobile = cert.Application?.ApplicantMobileNo ?? "",
            OfficerInputs = DeserializeOfficerInputs(cert.OfficerInputsJson),
            MergedHtmlContent = cert.MergedHtmlContent,
            QrCodePayload = cert.QrCodePayload,
            IssuedByUserId = cert.IssuedByUserId,
            IssuedByUserName = cert.IssuedByUser != null ? $"{cert.IssuedByUser.FirstName} {cert.IssuedByUser.LastName}".Trim() : "",
            IssuedByOfficerDesignation = cert.Application?.Department?.DepartmentNameLocal ?? cert.Application?.Department?.DepartmentName ?? "",
            IssuedAt = cert.IssuedAt,
            IsDigitallySigned = cert.IsDigitallySigned,
            DigitalSignatureInfo = cert.DigitalSignatureInfo
        };
    }

    private static Dictionary<string, string> DeserializeOfficerInputs(string? officerInputsJson)
    {
        if (string.IsNullOrWhiteSpace(officerInputsJson))
        {
            return new Dictionary<string, string>();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(officerInputsJson)
                ?? new Dictionary<string, string>();
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>();
        }
    }
}
