using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.Constants;
using NtisPlatform.Application.DTOs.RTSCertificate;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Enums;
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
    private readonly IHttpContextAccessor _httpContextAccessor;
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
        IHttpContextAccessor httpContextAccessor,
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
        _httpContextAccessor = httpContextAccessor;
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

        var certType = await ResolveCertificateTypeAsync(app, ct);
        if (certType == RTSCertificateType.None)
        {
            return new CertificatePreviewResponseDto
            {
                HasTemplate = false,
                TemplateId = 0,
                TemplateName = "No Certificate Required",
                SampleCertificateNo = string.Empty,
                MergedHtml = "<div class='p-4 text-center text-slate-500 font-semibold'>या सेवेसाठी कोणतेही प्रमाणपत्र जारी करण्याची आवश्यकता नाही (CertificateType=None).</div>",
                CertificateType = RTSCertificateType.None
            };
        }

        bool isManual = certType == RTSCertificateType.Manual;
        var template = isManual
            ? null
            : await _templateRepository.GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.ServiceId == app.ServiceId && t.IsActive && !t.MarkedForDeletion, ct);

        string deptCode = !string.IsNullOrWhiteSpace(app.Department?.DepartmentCode)
            ? app.Department.DepartmentCode.Trim()
            : (!string.IsNullOrWhiteSpace(app.Service?.Department?.DepartmentCode)
                ? app.Service.Department.DepartmentCode.Trim()
                : (!string.IsNullOrWhiteSpace(app.Department?.DepartmentName) && app.Department.DepartmentName.Length >= 2
                    ? app.Department.DepartmentName[..Math.Min(3, app.Department.DepartmentName.Length)].ToUpperInvariant()
                    : "SRV"));

        var response = new CertificatePreviewResponseDto
        {
            HasTemplate = isManual || template != null,
            TemplateId = template?.Id ?? 0,
            TemplateName = isManual ? "मॅन्युअल प्रमाणपत्र (Manual Upload Certificate)" : (template?.TemplateName ?? "Default Certificate Template"),
            SampleCertificateNo = $"CERT/{deptCode}/{DateTime.UtcNow:yyyy}/{app.Id:D6}",
            CertificateType = certType
        };

        if (!isManual && template != null)
        {
            var templateDto = MapToTemplateDto(template);
            response.RequiredOfficerFields = templateDto.OfficerFields;
            response.DefaultConditions = templateDto.DefaultConditions;
        }

        // Build Citizen Auto Values dictionary
        var autoValues = await BuildAutoValuesDictionaryAsync(app, ct);

        // Resolve designated approving officer details dynamically
        var (previewOfficerName, previewOfficerDesignation) = await ResolveOfficerDetailsAsync(app.Id, app.ServiceId, null, ct);
        if (!string.IsNullOrWhiteSpace(previewOfficerName))
        {
            autoValues["OfficerName"] = previewOfficerName;
            autoValues["ApprovedByOfficer"] = previewOfficerName;
        }
        if (!string.IsNullOrWhiteSpace(previewOfficerDesignation))
        {
            autoValues["OfficerDesignation"] = previewOfficerDesignation;
        }

        response.CitizenAutoValues = autoValues;

        // Perform merge
        if (isManual)
        {
            response.MergedHtml = "<div class='p-6 text-center text-slate-700 bg-amber-50/70 border border-amber-300 rounded-xl space-y-3'><div class='inline-flex p-3 bg-amber-100 rounded-full text-amber-700'>📋</div><h3 class='text-sm font-bold text-amber-900'>मॅन्युअल प्रमाणपत्र सेवा (Manual Certificate Service)</h3><p class='text-xs text-slate-600 max-w-md mx-auto'>सदर सेवेसाठी महानगरपालिकेमार्फत मॅन्युअली तयार केलेले प्रमाणपत्र (PDF किंवा स्कॅन प्रत) अपलोड केले जाते. डाव्या बाजूला फाईल निवडून अपलोड करा.</p><div class='p-3 bg-white border border-amber-200 rounded-lg text-[11px] font-semibold text-amber-800 inline-block'>⚠️ सूचना: नागरिकाने मूळ प्रमाणपत्र संबंधित विभागातून प्राप्त करून घेणे आवश्यक आहे.</div></div>";
            return response;
        }

        string rawHtml = BuildFullCertificateHtml(template, app);

        var verificationBaseUrl = await GetVerificationBaseUrlAsync(ct);
        string previewQrPayload = $"{verificationBaseUrl}/{app.ApplicationNo}";

        response.MergedHtml = MergeTemplatePlaceholders(
            rawHtml,
            autoValues,
            request.OfficerInputs,
            request.CustomConditions,
            response.SampleCertificateNo,
            previewOfficerName,
            isLiveSigned: false,
            certGuid: null,
            qrPayload: previewQrPayload,
            officerFieldsConfigJson: template?.OfficerFieldsConfigJson,
            defaultConditionsJson: template?.DefaultConditionsJson);

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

        var certType = request.CertificateType ?? await ResolveCertificateTypeAsync(app, ct);

        if (certType == RTSCertificateType.None)
        {
            throw new InvalidOperationException($"Service '{app.Service?.ServiceName}' is configured with CertificateType=None (No certificate issuance required).");
        }

        bool isManual = certType == RTSCertificateType.Manual;
        if (isManual && (!request.DocumentGuid.HasValue || request.DocumentGuid.Value == Guid.Empty))
        {
            throw new InvalidOperationException("मॅन्युअल प्रमाणपत्रासाठी फाईल (PDF/Image) अपलोड करणे बंधनकारक आहे.");
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
        var (officerName, officerDesignation) = await ResolveOfficerDetailsAsync(app.Id, app.ServiceId, userId, ct);
        if (!string.IsNullOrWhiteSpace(officerName))
        {
            autoValues["OfficerName"] = officerName;
            autoValues["ApprovedByOfficer"] = officerName;
        }
        if (!string.IsNullOrWhiteSpace(officerDesignation))
        {
            autoValues["OfficerDesignation"] = officerDesignation;
        }

        string rawHtml = isManual
            ? $@"<div class=""manual-certificate-view w-full max-w-4xl mx-auto bg-white p-6 rounded-xl border border-slate-300 shadow-sm"">
  <div class=""bg-amber-50 border border-amber-300 text-amber-900 px-4 py-3 rounded-lg mb-4 text-xs font-semibold"">
    ⚠️ <strong>महत्त्वाची सूचना:</strong> सदर मूळ अधिकृत प्रमाणपत्र संबंधित विभागामधून जमा (collect) करून घ्यावे.
  </div>
  <div class=""text-center my-4"">
    <p class=""text-sm font-bold text-slate-800 mb-2"">महानगरपालिकेकडून मॅन्युअली जारी केलेले अधिकृत प्रमाणपत्र</p>
    <a href=""/api/rts/documents/{request.DocumentGuid!.Value}/download"" target=""_blank"" class=""inline-flex items-center gap-2 px-5 py-2.5 bg-blue-600 hover:bg-blue-700 text-white font-bold text-xs rounded-xl shadow transition"">
      📄 मॅन्युअल प्रमाणपत्र पहा / डाऊनलोड करा
    </a>
  </div>
</div>"
            : BuildFullCertificateHtml(template, app);

        var signatureResult = !isManual
            ? _digitalSignatureService.SignCertificate(certNo, officerName, officerDesignation, rawHtml)
            : new CertificateSignatureResultDto { IsSigned = false, SignatureInfo = "मॅन्युअल प्रमाणपत्र (स्वाक्षरी लागू नाही)", SignatureHash = string.Empty };

        var verificationBaseUrl = await GetVerificationBaseUrlAsync(ct);
        string qrPayload = $"{verificationBaseUrl}/{certGuid}";

        string mergedHtml = isManual
            ? rawHtml
            : MergeTemplatePlaceholders(
                rawHtml,
                autoValues,
                request.OfficerInputs,
                request.CustomConditions,
                certNo,
                officerName,
                isLiveSigned: true,
                certGuid: certGuid,
                qrPayload: qrPayload,
                officerFieldsConfigJson: template?.OfficerFieldsConfigJson,
                defaultConditionsJson: template?.DefaultConditionsJson);

        if (existingCert != null)
        {
            existingCert.CertificateServiceId = template?.Id;
            existingCert.OfficerInputsJson = request.OfficerInputs != null && request.OfficerInputs.Count > 0 ? JsonSerializer.Serialize(request.OfficerInputs) : null;
            existingCert.MergedHtmlContent = mergedHtml;
            existingCert.QrCodePayload = qrPayload;
            existingCert.IssuedByUserId = userId;
            existingCert.IssuedAt = DateTime.UtcNow;
            existingCert.IsDigitallySigned = !isManual;
            existingCert.DigitalSignatureInfo = isManual ? null : signatureResult.SignatureInfo;
            existingCert.CertificateType = certType;
            existingCert.DocumentGuid = request.DocumentGuid;
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
                CertificateServiceId = template?.Id,
                OfficerInputsJson = request.OfficerInputs != null && request.OfficerInputs.Count > 0 ? JsonSerializer.Serialize(request.OfficerInputs) : null,
                MergedHtmlContent = mergedHtml,
                QrCodePayload = qrPayload,
                IssuedByUserId = userId,
                IssuedAt = DateTime.UtcNow,
                IsDigitallySigned = !isManual,
                DigitalSignatureInfo = isManual ? null : signatureResult.SignatureInfo,
                CertificateType = certType,
                DocumentGuid = request.DocumentGuid,
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
            Action = isManual ? "IssueManualCertificateAndApprove" : "IssueCertificateAndDigitalSign",
            Remark = !string.IsNullOrWhiteSpace(request.ActionRemark)
                ? request.ActionRemark
                : (isManual
                    ? $"मॅन्युअल प्रमाणपत्र क्र. {certNo} अधिकृतरीत्या अपलोड व जारी केले. (टीप: मूळ प्रमाणपत्र संबंधित विभागातून प्राप्त करून घेणे आवश्यक आहे)"
                    : $"प्रमाणपत्र क्र. {certNo} डिजिटल स्वाक्षरीने अधिकृतरीत्या जारी केले. (DSC Hash: {signatureResult.SignatureHash[..Math.Min(16, signatureResult.SignatureHash.Length)]}...)"),
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

    public async Task<CertificateVerificationResponseDto> VerifyCertificatePublicAsync(string identifier, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return new CertificateVerificationResponseDto
            {
                IsValid = false,
                Message = "सदर क्यूआर कोड किंवा प्रमाणपत्र क्रमांक अवैध आहे. (Invalid or Unverified Certificate)"
            };
        }

        var trimmed = identifier.Trim();
        bool isGuid = Guid.TryParse(trimmed, out var parsedGuid);
        int parsedAppId = 0;
        if (trimmed.StartsWith("RTS", StringComparison.OrdinalIgnoreCase))
        {
            _ = int.TryParse(trimmed.Substring(3).TrimStart('0'), out parsedAppId);
        }
        else
        {
            _ = int.TryParse(trimmed, out parsedAppId);
        }

        var query = _issuedCertRepository.GetQueryable()
            .AsNoTracking()
            .Include(c => c.Application)
            .Include(c => c.Service)
            .Include(c => c.IssuedByUser)
            .Where(c => !c.MarkedForDeletion);

        var cert = isGuid
            ? await query.FirstOrDefaultAsync(c => c.CertificateGuid == parsedGuid, ct)
            : await query.FirstOrDefaultAsync(c =>
                c.CertificateNo == trimmed ||
                (c.Application != null && c.Application.ApplicationNo == trimmed) ||
                (parsedAppId > 0 && c.ApplicationId == parsedAppId), ct);

        if (cert == null && !isGuid)
        {
            // fallback attempt by guid if string was a formatted guid
            if (Guid.TryParse(trimmed, out var fallbackGuid))
            {
                cert = await query.FirstOrDefaultAsync(c => c.CertificateGuid == fallbackGuid, ct);
            }
        }

        if (cert == null)
        {
            var app = await _applicationRepository.GetQueryable()
                .AsNoTracking()
                .Include(a => a.Service)
                .FirstOrDefaultAsync(a =>
                    a.ApplicationNo == trimmed ||
                    (parsedAppId > 0 && a.Id == parsedAppId), ct);

            if (app != null)
            {
                var ulbObj = await _ulbRepository.GetQueryable().AsNoTracking().FirstOrDefaultAsync(u => u.IsActive, ct)
                          ?? await _ulbRepository.GetQueryable().AsNoTracking().FirstOrDefaultAsync(ct);
                string ulbTitle = ulbObj?.UlbNameLocal ?? ulbObj?.UlbName ?? "";
                string ulbLogoPath = ulbObj?.UlbLogo ?? "";
                string ulbAddr = ulbObj?.UlbAddress ?? "";

                string deptTitle = "";
                if (app.Service != null)
                {
                    var d = await _departmentRepository.GetByIdAsync(app.Service.DepartmentId, ct);
                    deptTitle = d?.DepartmentNameLocal ?? d?.DepartmentName ?? "";
                }

                return new CertificateVerificationResponseDto
                {
                    IsValid = false,
                    CertificateGuid = Guid.Empty,
                    ApplicationNo = app.ApplicationNo,
                    ServiceName = app.Service?.ServiceNameLocal ?? app.Service?.ServiceName ?? "",
                    DepartmentName = deptTitle,
                    ApplicantName = app.ApplicantName ?? "",
                    UlbName = ulbTitle,
                    UlbLogo = ulbLogoPath,
                    UlbAddress = ulbAddr,
                    Message = $"सदर अर्ज क्र. {app.ApplicationNo} प्रणालीमध्ये अधिकृतरीत्या नोंदणीकृत असून सध्या प्रक्रियेत ({app.ApplicationStatus}) आहे. सक्षम प्राधिकाऱ्यांच्या अंतिम मंजुरीनंतर अधिकृत डिजिटल स्वाक्षरीसह प्रमाणपत्र उपलब्ध होईल."
                };
            }

            return new CertificateVerificationResponseDto
            {
                IsValid = false,
                CertificateGuid = isGuid ? parsedGuid : Guid.Empty,
                Message = "सदर क्यूआर कोड किंवा प्रमाणपत्र क्रमांक अवैध आहे. (Invalid or Unverified Certificate)"
            };
        }

        var ulb = await _ulbRepository.GetQueryable().AsNoTracking().FirstOrDefaultAsync(u => u.IsActive, ct)
               ?? await _ulbRepository.GetQueryable().AsNoTracking().FirstOrDefaultAsync(ct);
        string ulbName = ulb?.UlbNameLocal ?? ulb?.UlbName ?? "";
        string ulbLogo = ulb?.UlbLogo ?? "";
        string ulbAddress = ulb?.UlbAddress ?? "";

        string deptName = "";
        if (cert.Service != null)
        {
            var dept = await _departmentRepository.GetByIdAsync(cert.Service.DepartmentId, ct);
            deptName = dept?.DepartmentNameLocal ?? dept?.DepartmentName ?? "";
        }

        string officerName = cert.IssuedByUser != null
            ? (!string.IsNullOrWhiteSpace(cert.IssuedByUser.FirstName) || !string.IsNullOrWhiteSpace(cert.IssuedByUser.LastName)
                ? $"{cert.IssuedByUser.FirstName} {cert.IssuedByUser.LastName}".Trim()
                : cert.IssuedByUser.UserName)
            : "";
        string officerDesignation = "";

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
                officerDesignation = deptName;
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
            UlbLogo = ulbLogo,
            UlbAddress = ulbAddress,
            IssuedAt = cert.IssuedAt,
            IssuedByOfficer = officerName,
            OfficerDesignation = officerDesignation,
            IsDigitallySigned = cert.IsDigitallySigned,
            DigitalSignatureInfo = cert.DigitalSignatureInfo,
            DscSignerName = dscMetadata?.SignerName ?? ulbName,
            DscIssuer = dscMetadata?.Issuer ?? "",
            DscSerialNumber = dscMetadata?.SerialNumber ?? "",
            DscThumbprint = dscMetadata?.Thumbprint ?? "",
            DscValidUntil = dscMetadata?.ValidTo,
            MergedHtmlContent = cert.MergedHtmlContent,
            CertificateType = cert.CertificateType,
            DocumentGuid = cert.DocumentGuid,
            DocumentDownloadUrl = cert.DocumentGuid.HasValue && cert.DocumentGuid.Value != Guid.Empty
                ? $"/api/rts/documents/{cert.DocumentGuid.Value}/download"
                : null,
            DepartmentCollectionNotice = cert.CertificateType == RTSCertificateType.Manual
                ? "सदर मूळ अधिकृत प्रमाणपत्र संबंधित विभागामधून जमा (collect) करून घ्यावे."
                : null,
            Message = cert.CertificateType == RTSCertificateType.Manual
                ? "✅ हे मॅन्युअल प्रमाणपत्र अधिकृतरीत्या पडताळलेले आहे. (सदर मूळ अधिकृत प्रमाणपत्र संबंधित विभागामधून जमा करून घ्यावे.)"
                : "✅ हे प्रमाणपत्र अधिकृतरीत्या पडताळलेले व अस्सल आहे. (Officially Verified & Authentic Certificate)"
        };
    }

    private async Task<string> GetVerificationBaseUrlAsync(CancellationToken ct)
    {
        try
        {
            var ulb = await _ulbRepository.GetQueryable().FirstOrDefaultAsync(u => u.IsActive, ct);
            if (!string.IsNullOrWhiteSpace(ulb?.WebsiteUrl) && !ulb.WebsiteUrl.Trim().Equals("-"))
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

        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                var origin = httpContext.Request.Headers["Origin"].FirstOrDefault()
                          ?? httpContext.Request.Headers["Referer"].FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(origin) && Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                {
                    return $"{uri.Scheme}://{uri.Authority}/service/verify-certificate";
                }

                if (httpContext.Request.Host.HasValue)
                {
                    return $"{httpContext.Request.Scheme}://{httpContext.Request.Host.Value}/service/verify-certificate";
                }
            }
        }
        catch { }

        return "/service/verify-certificate";
    }

    private static readonly JsonSerializerOptions JsonCaseInsensitiveOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Helper Methods
    private async Task<Dictionary<string, string>> BuildAutoValuesDictionaryAsync(RTSApplicationDetailsEntity app, CancellationToken ct)
    {
        var ulb = await _ulbRepository.GetQueryable().AsNoTracking().FirstOrDefaultAsync(ct);

        var currentDate = DateTime.UtcNow.Date;
        string appDate = (app.CreatedDate ?? currentDate).ToString("dd/MM/yyyy");
        string issueDate = currentDate.ToString("dd/MM/yyyy");
        string srvNameMarathi = app.Service?.ServiceNameLocal ?? app.Service?.ServiceName ?? "";
        string srvNameEng = app.Service?.ServiceName ?? "";
        string deptNameMarathi = app.Department?.DepartmentNameLocal ?? app.Department?.DepartmentName ?? "";
        string ulbNameMarathi = ulb?.UlbNameLocal ?? ulb?.UlbName ?? "";
        string ulbNameEng = ulb?.UlbName ?? "";
        string ulbAddress = ulb?.UlbAddress ?? "";
        string ulbMobile = ulb?.MobileNo ?? "";
        string ulbEmail = ulb?.EmailId ?? "";
        string ulbWebsite = ulb?.WebsiteUrl ?? "";

        string ulbCode = ulb?.UlbCode ?? "";
        string ulbShortCode = !string.IsNullOrWhiteSpace(ulb?.UlbNameLocal) ? ulb.UlbNameLocal.Split(' ').FirstOrDefault() ?? "" : "";
        string currentYear = DateTime.UtcNow.Year.ToString();

        string deptCode = !string.IsNullOrWhiteSpace(app.Department?.DepartmentCode)
            ? app.Department.DepartmentCode.Trim()
            : (!string.IsNullOrWhiteSpace(app.Service?.Department?.DepartmentCode)
                ? app.Service.Department.DepartmentCode.Trim()
                : (!string.IsNullOrWhiteSpace(app.Department?.DepartmentName) && app.Department.DepartmentName.Length >= 2
                    ? app.Department.DepartmentName[..Math.Min(3, app.Department.DepartmentName.Length)].ToUpperInvariant()
                    : ""));

        string serviceCode = !string.IsNullOrWhiteSpace(app.Service?.ServiceCode)
            ? app.Service.ServiceCode.Trim()
            : $"SRV{app.ServiceId:D3}";

        string standardOutwardNo = !string.IsNullOrWhiteSpace(ulbShortCode) && !string.IsNullOrWhiteSpace(deptCode)
            ? $"{ulbShortCode}/{deptCode}/{currentYear}/{app.Id:D6}"
            : $"{app.Id:D6}";

        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ApplicationNo"] = app.ApplicationNo ?? $"RTS{app.Id:D8}",
            ["ApplicantName"] = app.ApplicantName ?? "",
            ["ApplicantMobile"] = app.ApplicantMobileNo ?? "",
            ["ApplicantAddress"] = "",
            ["ULBLogo"] = ulb?.UlbLogo ?? "",
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

                    // Common synonyms for Education, Student, School Leaving
                    if (code.Contains("Mother", StringComparison.OrdinalIgnoreCase) || code.Contains("आई", StringComparison.OrdinalIgnoreCase))
                    {
                        dict["MotherName"] = val;
                    }
                    if (code.Contains("Father", StringComparison.OrdinalIgnoreCase) || code.Contains("वडील", StringComparison.OrdinalIgnoreCase))
                    {
                        dict["FatherName"] = val;
                    }
                    if (code.Contains("Student", StringComparison.OrdinalIgnoreCase) || code.Contains("विद्यार्थी", StringComparison.OrdinalIgnoreCase))
                    {
                        dict["StudentName"] = val;
                        if (string.IsNullOrWhiteSpace(app.ApplicantName) || app.ApplicantName == "सन्माननीय नागरिक")
                            dict["ApplicantName"] = val;
                    }
                    if (code.Equals("dateOfBirth", StringComparison.OrdinalIgnoreCase) || code.Contains("DOB", StringComparison.OrdinalIgnoreCase) || code.Contains("जन्म", StringComparison.OrdinalIgnoreCase))
                    {
                        dict["DOB"] = val;
                        dict["DateOfBirth"] = val;
                    }
                    if (code.Contains("Caste", StringComparison.OrdinalIgnoreCase) || code.Contains("जात", StringComparison.OrdinalIgnoreCase) || code.Contains("प्रवर्ग", StringComparison.OrdinalIgnoreCase))
                    {
                        dict["CasteCategory"] = val;
                        dict["Caste"] = val;
                    }
                    if (code.Contains("BirthPlace", StringComparison.OrdinalIgnoreCase) || code.Contains("जन्मस्थान", StringComparison.OrdinalIgnoreCase))
                    {
                        dict["BirthPlace"] = val;
                    }
                    if (code.Contains("School", StringComparison.OrdinalIgnoreCase) || code.Contains("शाळा", StringComparison.OrdinalIgnoreCase))
                    {
                        dict["SchoolName"] = val;
                    }
                    if (code.Contains("Standard", StringComparison.OrdinalIgnoreCase) || code.Contains("इयत्ता", StringComparison.OrdinalIgnoreCase) || code.Contains("Class", StringComparison.OrdinalIgnoreCase))
                    {
                        dict["StandardStudied"] = val;
                        dict["LastStandardStudied"] = val;
                    }
                    if (code.Contains("Reason", StringComparison.OrdinalIgnoreCase) || code.Contains("कारण", StringComparison.OrdinalIgnoreCase))
                    {
                        dict["ReasonForLeaving"] = val;
                        dict["LeavingReason"] = val;
                    }
                    if (code.Contains("YearOfLeaving", StringComparison.OrdinalIgnoreCase) || code.Contains("LeavingYear", StringComparison.OrdinalIgnoreCase))
                    {
                        dict["LeavingDate"] = val;
                        dict["LeavingYear"] = val;
                    }

                    // Common Property, Zone, Ward, Address synonyms
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

    private async Task<(string OfficerName, string OfficerDesignation)> ResolveOfficerDetailsAsync(int applicationId, int? serviceId, int? userId, CancellationToken ct)
    {
        string officerName = string.Empty;
        string officerDesignation = string.Empty;

        // 1. If explicit userId provided (e.g. from issuing officer or logged in user)
        if (userId.HasValue && userId.Value > 0)
        {
            var u = await _userRepository.GetByIdAsync(userId.Value, ct);
            if (u != null)
            {
                officerName = !string.IsNullOrWhiteSpace(u.FirstName) || !string.IsNullOrWhiteSpace(u.LastName)
                    ? $"{u.FirstName} {u.LastName}".Trim()
                    : (u.UserName ?? string.Empty);

                var roleAlloc = await _userRoleAllocationRepository.GetQueryable()
                    .AsNoTracking()
                    .Include(r => r.UserRole)
                    .FirstOrDefaultAsync(r => r.UserId == u.Id && r.IsActive, ct);

                if (!string.IsNullOrWhiteSpace(roleAlloc?.UserRole?.UserRoleName))
                {
                    officerDesignation = roleAlloc.UserRole.UserRoleName;
                }
            }
        }

        // 2. If not resolved, check ApprovalFlowStageMaster for this service's final or certificate issuance stage
        if (string.IsNullOrWhiteSpace(officerName) && serviceId.HasValue && serviceId.Value > 0)
        {
            var designatedStage = await _stageRepository.GetQueryable()
                .AsNoTracking()
                .Include(s => s.ApprovalFlow)
                .Where(s => s.ApprovalFlow.ServiceId == serviceId.Value && (s.IsFinalStage || s.CanIssueCertificate) && s.UserId > 0)
                .OrderByDescending(s => s.IsFinalStage)
                .ThenByDescending(s => s.CanIssueCertificate)
                .FirstOrDefaultAsync(ct);

            if (designatedStage != null && designatedStage.UserId > 0)
            {
                var designatedUser = await _userRepository.GetByIdAsync(designatedStage.UserId, ct);
                if (designatedUser != null)
                {
                    officerName = !string.IsNullOrWhiteSpace(designatedUser.FirstName) || !string.IsNullOrWhiteSpace(designatedUser.LastName)
                        ? $"{designatedUser.FirstName} {designatedUser.LastName}".Trim()
                        : (designatedUser.UserName ?? string.Empty);

                    officerDesignation = !string.IsNullOrWhiteSpace(designatedStage.StageName)
                        ? designatedStage.StageName
                        : string.Empty;

                    if (string.IsNullOrWhiteSpace(officerDesignation))
                    {
                        var roleAlloc = await _userRoleAllocationRepository.GetQueryable()
                            .AsNoTracking()
                            .Include(r => r.UserRole)
                            .FirstOrDefaultAsync(r => r.UserId == designatedUser.Id && r.IsActive, ct);
                        if (!string.IsNullOrWhiteSpace(roleAlloc?.UserRole?.UserRoleName))
                        {
                            officerDesignation = roleAlloc.UserRole.UserRoleName;
                        }
                    }
                }
            }
        }

        // 3. If still not resolved, check TrackApplicationHistory for this application
        if (string.IsNullOrWhiteSpace(officerName) && applicationId > 0)
        {
            var lastAction = await _historyRepository.GetQueryable()
                .AsNoTracking()
                .Where(h => h.ApplicationId == applicationId && h.ActionByUserId > 0)
                .OrderByDescending(h => h.CreatedDate)
                .FirstOrDefaultAsync(ct);

            if (lastAction != null && lastAction.ActionByUserId.HasValue && lastAction.ActionByUserId.Value > 0)
            {
                var historyUser = await _userRepository.GetByIdAsync(lastAction.ActionByUserId.Value, ct);
                if (historyUser != null)
                {
                    officerName = !string.IsNullOrWhiteSpace(historyUser.FirstName) || !string.IsNullOrWhiteSpace(historyUser.LastName)
                        ? $"{historyUser.FirstName} {historyUser.LastName}".Trim()
                        : (historyUser.UserName ?? string.Empty);

                    var roleAlloc = await _userRoleAllocationRepository.GetQueryable()
                        .AsNoTracking()
                        .Include(r => r.UserRole)
                        .FirstOrDefaultAsync(r => r.UserId == historyUser.Id && r.IsActive, ct);
                    if (!string.IsNullOrWhiteSpace(roleAlloc?.UserRole?.UserRoleName))
                    {
                        officerDesignation = roleAlloc.UserRole.UserRoleName;
                    }
                }
            }
        }

        // 4. Default dynamic fallback
        if (string.IsNullOrWhiteSpace(officerName))
        {
            var adminUser = await _userRepository.GetQueryable()
                .AsNoTracking()
                .Where(u => u.IsActive && !u.MarkedForDeletion)
                .OrderBy(u => u.Id)
                .FirstOrDefaultAsync(ct);
            if (adminUser != null)
            {
                officerName = $"{adminUser.FirstName} {adminUser.LastName}".Trim();
                officerDesignation = "विभाग प्रमुख";
            }
        }

        return (officerName, officerDesignation);
    }

    private string MergeTemplatePlaceholders(
        string rawTemplateHtml,
        Dictionary<string, string> citizenValues,
        Dictionary<string, string>? officerInputs,
        string? customConditions,
        string? sampleCertNo,
        string? officerName,
        bool isLiveSigned,
        Guid? certGuid = null,
        string? qrPayload = null,
        string? officerFieldsConfigJson = null,
        string? defaultConditionsJson = null)
    {
        string html = rawTemplateHtml;

        // Extract friendly labels for officer fields if available
        Dictionary<string, string> fieldLabels = new(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(officerFieldsConfigJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(officerFieldsConfigJson);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var el in doc.RootElement.EnumerateArray())
                    {
                        string key = "";
                        if (el.TryGetProperty("fieldKey", out var fk) || el.TryGetProperty("FieldKey", out fk))
                            key = fk.GetString() ?? "";

                        string label = "";
                        if (el.TryGetProperty("fieldLabelMarathi", out var flm) || el.TryGetProperty("FieldLabelMarathi", out flm))
                            label = flm.GetString() ?? "";
                        if (string.IsNullOrWhiteSpace(label) && (el.TryGetProperty("fieldLabelEnglish", out var fle) || el.TryGetProperty("FieldLabelEnglish", out fle)))
                            label = fle.GetString() ?? "";

                        if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(label))
                        {
                            fieldLabels[key] = label;
                        }
                    }
                }
            }
            catch { }
        }

        // 1. Replace Citizen dynamic fields {{TagName}} AND [[TagName]]
        foreach (var (k, v) in citizenValues)
        {
            if (!string.IsNullOrWhiteSpace(v))
            {
                html = html.Replace($"{{{{{k}}}}}", v, StringComparison.OrdinalIgnoreCase);
                html = html.Replace($"[[{k}]]", v, StringComparison.OrdinalIgnoreCase);
            }
        }

        // Common system tags
        html = html.Replace("{{CertificateNo}}", sampleCertNo ?? "", StringComparison.OrdinalIgnoreCase);
        html = html.Replace("[[CertificateNo]]", sampleCertNo ?? "", StringComparison.OrdinalIgnoreCase);
        html = html.Replace("{{OfficerName}}", officerName ?? "", StringComparison.OrdinalIgnoreCase);
        html = html.Replace("[[OfficerName]]", officerName ?? "", StringComparison.OrdinalIgnoreCase);
        html = html.Replace("{{ApprovedByOfficer}}", officerName ?? "", StringComparison.OrdinalIgnoreCase);
        html = html.Replace("[[ApprovedByOfficer]]", officerName ?? "", StringComparison.OrdinalIgnoreCase);
        string dynamicDesignation = citizenValues.GetValueOrDefault("OfficerDesignation") ?? citizenValues.GetValueOrDefault("DepartmentName") ?? "";
        html = html.Replace("{{OfficerDesignation}}", dynamicDesignation, StringComparison.OrdinalIgnoreCase);
        html = html.Replace("[[OfficerDesignation]]", dynamicDesignation, StringComparison.OrdinalIgnoreCase);
        string todayFormatted = DateTime.UtcNow.ToString("dd/MM/yyyy");
        html = html.Replace("{{ApprovalDate}}", todayFormatted, StringComparison.OrdinalIgnoreCase);
        html = html.Replace("[[ApprovalDate]]", todayFormatted, StringComparison.OrdinalIgnoreCase);
        html = html.Replace("{{IssueDate}}", todayFormatted, StringComparison.OrdinalIgnoreCase);
        html = html.Replace("[[IssueDate]]", todayFormatted, StringComparison.OrdinalIgnoreCase);
        html = html.Replace("{{CurrentDate}}", todayFormatted, StringComparison.OrdinalIgnoreCase);
        html = html.Replace("[[CurrentDate]]", todayFormatted, StringComparison.OrdinalIgnoreCase);
        html = html.Replace("{{DocumentDate}}", todayFormatted, StringComparison.OrdinalIgnoreCase);
        html = html.Replace("[[DocumentDate]]", todayFormatted, StringComparison.OrdinalIgnoreCase);
        html = html.Replace("{{Today}}", todayFormatted, StringComparison.OrdinalIgnoreCase);
        html = html.Replace("[[Today]]", todayFormatted, StringComparison.OrdinalIgnoreCase);

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
                    html = html.Replace($"{{{{{k}}}}}", v, StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        // Automatic Dynamic Fallback for OutwardNo / OrderNo if left blank by officer
        string fallbackOutward = citizenValues.GetValueOrDefault("OutwardNo") ?? sampleCertNo ?? $"OUT/RTS/{DateTime.UtcNow:yyyy}";
        html = html.Replace("[[OutwardNo]]", fallbackOutward, StringComparison.OrdinalIgnoreCase);
        html = html.Replace("[[OrderNo]]", fallbackOutward, StringComparison.OrdinalIgnoreCase);
        html = html.Replace("{{OutwardNo}}", fallbackOutward, StringComparison.OrdinalIgnoreCase);
        html = html.Replace("{{OrderNo}}", fallbackOutward, StringComparison.OrdinalIgnoreCase);

        // Check if customConditions provided, or fallback to default conditions from template
        string conditionListHtml = "";
        if (!string.IsNullOrWhiteSpace(customConditions))
        {
            var conditionLines = customConditions.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var formattedConditions = string.Join("", conditionLines.Select(c => $"<li>{c.Trim()}</li>"));
            conditionListHtml = $"<ol class='list-decimal pl-6 text-xs text-slate-900 space-y-1.5 leading-normal'>{formattedConditions}</ol>";
        }
        else if (!string.IsNullOrWhiteSpace(defaultConditionsJson))
        {
            try
            {
                var defConditions = JsonSerializer.Deserialize<List<string>>(defaultConditionsJson);
                if (defConditions != null && defConditions.Count > 0)
                {
                    var formattedConditions = string.Join("", defConditions.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => $"<li>{c.Trim()}</li>"));
                    conditionListHtml = $"<ol class='list-decimal pl-6 text-xs text-slate-900 space-y-1.5 leading-normal'>{formattedConditions}</ol>";
                }
            }
            catch { }
        }

        html = html.Replace("{{CustomConditionsList}}", conditionListHtml, StringComparison.OrdinalIgnoreCase);
        html = html.Replace("[[CustomConditionsList]]", conditionListHtml, StringComparison.OrdinalIgnoreCase);
        html = html.Replace("{{DefaultConditionsList}}", conditionListHtml, StringComparison.OrdinalIgnoreCase);
        html = html.Replace("[[SpecialConditions]]", customConditions ?? "", StringComparison.OrdinalIgnoreCase);
        html = html.Replace("{{SpecialConditions}}", customConditions ?? "", StringComparison.OrdinalIgnoreCase);

        // Dynamically build and inject {{OfficerFieldsBlock}} if present or if officer inputs exist
        if (officerInputs != null && officerInputs.Count > 0)
        {
            var filledInputs = officerInputs
                .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
                .ToList();
            if (filledInputs.Count > 0)
            {
                var officerBlockRows = string.Join("", filledInputs.Select(kv => {
                    string label = string.Equals(kv.Key, "OfficerRemark", StringComparison.OrdinalIgnoreCase) || string.Equals(kv.Key, "OfficerRemarks", StringComparison.OrdinalIgnoreCase)
                        ? "अधिकाऱ्याचा शेरा (Officer Remark)"
                        : (fieldLabels.GetValueOrDefault(kv.Key) ?? kv.Key);
                    return $@"
                    <tr class='border-b border-slate-200'>
                        <td class='p-2 font-bold text-slate-700 w-1/3 bg-slate-50 border-r border-slate-200'>{label}:</td>
                        <td class='p-2 text-slate-900 font-semibold'>{kv.Value}</td>
                    </tr>";
                }));

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
                else if (html.Contains("[[OfficerFieldsBlock]]", StringComparison.OrdinalIgnoreCase))
                {
                    html = html.Replace("[[OfficerFieldsBlock]]", dynamicOfficerBlock, StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        html = html.Replace("{{OfficerFieldsBlock}}", "", StringComparison.OrdinalIgnoreCase);
        html = html.Replace("[[OfficerFieldsBlock]]", "", StringComparison.OrdinalIgnoreCase);
        html = Regex.Replace(html, @"\[\[\w+\]\]", "");

        // 3. Inject Dynamic Official Seal Stamp, QR Code and Digital Signature Blocks
        string ulbLogoUrl = citizenValues.GetValueOrDefault("ULBLogo") ?? "";
        string sealStampHtml = !string.IsNullOrWhiteSpace(ulbLogoUrl) ? $@"
        <div class='official-seal-stamp inline-block text-center'>
            <img src='{WebUtility.HtmlEncode(ulbLogoUrl)}' alt='' class='w-28 h-28 object-contain transform -rotate-6 filter drop-shadow-xs inline-block' onerror=""this.style.display='none'"" />
        </div>" : "";

        string actualQr = !string.IsNullOrWhiteSpace(qrPayload)
            ? qrPayload
            : (certGuid.HasValue && certGuid.Value != Guid.Empty
                ? $"/service/verify-certificate/{certGuid.Value}"
                : (!string.IsNullOrWhiteSpace(sampleCertNo) ? $"/service/verify-certificate/{Uri.EscapeDataString(sampleCertNo)}" : ""));

        string qrCodeHtml = !string.IsNullOrWhiteSpace(actualQr) ? $@"
        <div class='inline-flex flex-col items-center justify-center p-1.5 bg-white border border-slate-300 rounded shadow-xs text-center' style='width: 80px;' title='{WebUtility.HtmlEncode(actualQr)}'>
            <div style='width: 64px; height: 64px;' class='flex items-center justify-center bg-white'>
                <img src='https://api.qrserver.com/v1/create-qr-code/?size=150x150&data={Uri.EscapeDataString(actualQr)}' alt='Scan to Verify' class='w-full h-full object-contain' />
            </div>
            <span class='text-slate-700 mt-0.5 font-bold' style='font-size: 8px;'>Scan to Verify</span>
        </div>" : "";

        string officerDesignationDynamic = citizenValues.GetValueOrDefault("OfficerDesignation") ?? citizenValues.GetValueOrDefault("DepartmentName") ?? "";
        string signatureHtml = _digitalSignatureService.GenerateSignatureHtml(officerName, officerDesignationDynamic, DateTime.UtcNow, sampleCertNo ?? "");

        // Seal stamp replacement
        var sealRegex = new Regex(@"(?i)(?:\{\{|\{\s*|\[\[)\s*(?:OfficialSealStamp|SealStamp|ULBSeal|Stamp)\s*(?:\}\}|\s*\}|\]\])");
        html = sealRegex.Replace(html, sealStampHtml);

        // QR Code replacement
        var qrRegex = new Regex(@"(?i)(?:\{\{|\{\s*|\[\[)\s*(?:QRCode(?:Text)?|QR_Code|VerifyQR)\s*(?:\}\}|\s*\}|\]\])");
        html = qrRegex.Replace(html, qrCodeHtml);

        // 1. Replace all variations of {{DigitalSignature}} and [[DigitalSignature]]
        var sigTagRegex = new Regex(@"(?i)(?:\{\{|\{\s*|\[\[)\s*(?:DigitalSignature(?:Text)?|Digital_Signature|digitalSignature|OfficerSignature|Signature|DSC)\s*(?:\}\}|\s*\}|\]\])");
        bool signaturePlaced = false;
        if (sigTagRegex.IsMatch(html))
        {
            html = sigTagRegex.Replace(html, signatureHtml);
            signaturePlaced = true;
        }

        // 2. Replace any mockup/old .digital-signature-card inside template HTML ONLY if not placed via tag
        if (!signaturePlaced)
        {
            var mockCardRegex = new Regex(@"(?i)<div[^>]*class=['""][^'""]*digital-signature-card[^'""]*['""][^>]*>[\s\S]*?<\/div>(?:\s*<\/div>)*");
            if (mockCardRegex.IsMatch(html))
            {
                html = mockCardRegex.Replace(html, signatureHtml);
                signaturePlaced = true;
            }
        }

        // 3. If template has .right-digital-sign block, ensure dynamic signature is placed inside
        if (!signaturePlaced)
        {
            var rightSignRegex = new Regex(@"(?i)(<div[^>]*class=['""][^'""]*right-digital-sign[^'""]*['""][^>]*>)([\s\S]*?)(<\/div>)");
            if (rightSignRegex.IsMatch(html))
            {
                html = rightSignRegex.Replace(html, $"$1\n{signatureHtml}\n$3");
                signaturePlaced = true;
            }
        }

        // 4. Fallback: If still not placed anywhere, append cleanly before closing body or at end
        if (!signaturePlaced)
        {
            if (html.Contains("</body>", StringComparison.OrdinalIgnoreCase))
            {
                html = html.Replace("</body>", $"<div class='text-right mt-4'>{signatureHtml}</div></body>", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                html += $"<div class='text-right mt-4'>{signatureHtml}</div>";
            }
        }

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
                string json = entity.OfficerFieldsConfigJson.Trim();
                if (json.StartsWith("\"") && json.EndsWith("\""))
                {
                    try { json = JsonSerializer.Deserialize<string>(json) ?? json; } catch {}
                }
                if (json.Contains("\\\""))
                {
                    json = json.Replace("\\\"", "\"");
                }
                dto.OfficerFields = JsonSerializer.Deserialize<List<OfficerFieldConfigDto>>(json, JsonCaseInsensitiveOptions) ?? new();
            }
            catch {}
        }

        if (!string.IsNullOrWhiteSpace(entity.DefaultConditionsJson))
        {
            try
            {
                string json = entity.DefaultConditionsJson.Trim();
                if (json.StartsWith("\"") && json.EndsWith("\""))
                {
                    try { json = JsonSerializer.Deserialize<string>(json) ?? json; } catch {}
                }
                if (json.Contains("\\\""))
                {
                    json = json.Replace("\\\"", "\"");
                }
                dto.DefaultConditions = JsonSerializer.Deserialize<List<string>>(json, JsonCaseInsensitiveOptions) ?? new();
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
            DigitalSignatureInfo = cert.DigitalSignatureInfo,
            CertificateType = cert.CertificateType,
            DocumentGuid = cert.DocumentGuid,
            DocumentDownloadUrl = cert.DocumentGuid.HasValue && cert.DocumentGuid.Value != Guid.Empty
                ? $"/api/rts/documents/{cert.DocumentGuid.Value}/download"
                : null,
            DepartmentCollectionNotice = cert.CertificateType == RTSCertificateType.Manual
                ? "सदर मूळ अधिकृत प्रमाणपत्र संबंधित विभागामधून जमा (collect) करून घ्यावे."
                : null
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

    private static Task<RTSCertificateType> ResolveCertificateTypeAsync(RTSApplicationDetailsEntity app, CancellationToken ct)
    {
        if (app.Service == null) return Task.FromResult(RTSCertificateType.None);
        return Task.FromResult(app.Service.CertificateType);
    }
}
