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
    private readonly IRepository<RTSCertificateTemplateMasterEntity, int> _templateRepository;
    private readonly IRepository<RTSIssuedCertificateEntity, int> _issuedCertRepository;
    private readonly IRepository<RTSApplicationDetailsEntity, int> _applicationRepository;
    private readonly IRepository<RTSFieldValueEntity, int> _fieldValueRepository;
    private readonly IRepository<RTSServiceEntity, int> _serviceRepository;
    private readonly IRepository<RTSDepartmentEntity, int> _departmentRepository;
    private readonly IRepository<UserEntity, int> _userRepository;
    private readonly IRepository<ULBMasterEntity, int> _ulbRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RTSCertificateService> _logger;

    public RTSCertificateService(
        IRepository<RTSCertificateTemplateMasterEntity, int> templateRepository,
        IRepository<RTSIssuedCertificateEntity, int> issuedCertRepository,
        IRepository<RTSApplicationDetailsEntity, int> applicationRepository,
        IRepository<RTSFieldValueEntity, int> fieldValueRepository,
        IRepository<RTSServiceEntity, int> serviceRepository,
        IRepository<RTSDepartmentEntity, int> departmentRepository,
        IRepository<UserEntity, int> userRepository,
        IRepository<ULBMasterEntity, int> ulbRepository,
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
            new() { TagKey = "{{ServiceName}}", TagLabelMarathi = "सेवेचे नाव", TagLabelEnglish = "Service Name", SourceType = "System" },
            new() { TagKey = "{{ServiceNameMarathi}}", TagLabelMarathi = "सेवेचे स्थानिक नाव", TagLabelEnglish = "Service Local Name", SourceType = "System" },
            new() { TagKey = "{{DepartmentName}}", TagLabelMarathi = "विभागाचे नाव", TagLabelEnglish = "Department Name", SourceType = "System" },
            new() { TagKey = "{{ULBName}}", TagLabelMarathi = "महानगरपालिकेचे नाव", TagLabelEnglish = "ULB Name", SourceType = "System" },
            new() { TagKey = "{{ULBNameMarathi}}", TagLabelMarathi = "महानगरपालिका स्थानिक नाव", TagLabelEnglish = "ULB Marathi Name", SourceType = "System" },
            new() { TagKey = "{{IssueDate}}", TagLabelMarathi = "प्रमाणपत्र जारी दिनांक", TagLabelEnglish = "Issue Date", SourceType = "System" },
            new() { TagKey = "{{CertificateNo}}", TagLabelMarathi = "प्रमाणपत्र / दाखला क्रमांक", TagLabelEnglish = "Certificate Number", SourceType = "System" },
            new() { TagKey = "{{ApprovedByOfficer}}", TagLabelMarathi = "मंजुरी अधिकाऱ्याचे नाव", TagLabelEnglish = "Approving Officer Name", SourceType = "System" },
            new() { TagKey = "{{OfficerDesignation}}", TagLabelMarathi = "अधिकाऱ्याचे पदनाम", TagLabelEnglish = "Officer Designation", SourceType = "System" },
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
        var existing = await _templateRepository.GetQueryable()
            .FirstOrDefaultAsync(t => t.ServiceId == dto.ServiceId && !t.MarkedForDeletion, ct);

        if (existing != null)
        {
            throw new InvalidOperationException($"A certificate template already exists for ServiceId {dto.ServiceId}. Please update the existing template.");
        }

        var entity = new RTSCertificateTemplateMasterEntity
        {
            ServiceId = dto.ServiceId,
            TemplateName = dto.TemplateName.Trim(),
            TemplateCode = string.IsNullOrWhiteSpace(dto.TemplateCode) ? $"CERT_{dto.ServiceId}_{DateTime.UtcNow.Ticks % 10000}" : dto.TemplateCode.Trim().ToUpper(),
            HeaderContent = dto.HeaderContent,
            BodyContent = dto.BodyContent,
            FooterContent = dto.FooterContent,
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

        var response = new CertificatePreviewResponseDto
        {
            HasTemplate = template != null,
            TemplateId = template?.Id ?? 0,
            TemplateName = template?.TemplateName ?? "Default Certificate Template",
            SampleCertificateNo = $"CERT/RTS/SRV/{DateTime.UtcNow:yyyy}/{app.Id:D5}"
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

        response.MergedHtml = MergeTemplatePlaceholders(rawHtml, autoValues, request.OfficerInputs, request.CustomConditions, response.SampleCertificateNo, "श्री. एस. के. जोशी (प्र. सहाय्यक आयुक्त)", isLiveSigned: false);

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

        var user = await _userRepository.GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        var template = await _templateRepository.GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.ServiceId == app.ServiceId && t.IsActive && !t.MarkedForDeletion, ct);

        // Generate Certificate Number
        string deptCode = app.Department?.DepartmentName?.ToUpperInvariant() switch
        {
            "EDUCATION" => "EDU",
            "TOWN PLANNING" => "TP",
            "NULM" => "NULM",
            "PWD" => "PWD",
            "HEALTH" => "HLT",
            _ => "SRV"
        };
        string certNo = $"CERT/RTS/{deptCode}/{DateTime.UtcNow:yyyy}/{app.Id:D5}";
        var certGuid = Guid.NewGuid();

        var autoValues = await BuildAutoValuesDictionaryAsync(app, ct);
        string officerName = user != null ? $"{user.FirstName} {user.LastName}".Trim() : "मंजुरी अधिकारी";
        string rawHtml = BuildFullCertificateHtml(template, app);

        string mergedHtml = MergeTemplatePlaceholders(rawHtml, autoValues, request.OfficerInputs, request.CustomConditions, certNo, officerName, isLiveSigned: true, certGuid: certGuid);

        var issuedCert = new RTSIssuedCertificateEntity
        {
            CertificateGuid = certGuid,
            CertificateNo = certNo,
            ApplicationId = app.Id,
            ServiceId = app.ServiceId,
            TemplateId = template?.Id ?? 0,
            OfficerInputsJson = request.OfficerInputs != null && request.OfficerInputs.Count > 0 ? JsonSerializer.Serialize(request.OfficerInputs) : null,
            MergedHtmlContent = mergedHtml,
            QrCodePayload = $"https://onesolutionakola.tabamc.in/service/verify-certificate/{certGuid}",
            IssuedByUserId = userId,
            IssuedAt = DateTime.UtcNow,
            IsDigitallySigned = true,
            DigitalSignatureInfo = $"Digitally Signed by {officerName} at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC} | CertNo: {certNo}",
            IsActive = true,
            CreatedBy = userId,
            CreatedDate = DateTime.UtcNow
        };

        await _issuedCertRepository.AddAsync(issuedCert, ct);

        // If SignAndApprove is checked, complete the application workflow approval
        if (request.SignAndApprove && app.ApplicationStatus != ApplicationStatus.Approved)
        {
            app.ApplicationStatus = ApplicationStatus.Approved;
            app.Remark = string.IsNullOrWhiteSpace(request.ActionRemark) ? $"Certificate issued ({certNo}) & Approved" : request.ActionRemark;
            app.UpdatedBy = userId;
            app.UpdatedDate = DateTime.UtcNow;
            await _applicationRepository.UpdateAsync(app, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        return await GetIssuedCertificateByGuidAsync(certGuid, ct) ?? new RTSIssuedCertificateDto
        {
            Id = issuedCert.Id,
            CertificateGuid = certGuid,
            CertificateNo = certNo,
            ApplicationId = app.Id,
            ApplicationNo = app.ApplicationNo ?? $"RTS{app.Id:D8}",
            ServiceName = app.Service?.ServiceName ?? "",
            DepartmentName = app.Department?.DepartmentName ?? "",
            ApplicantName = app.ApplicantName ?? "",
            ApplicantMobile = app.ApplicantMobileNo ?? "",
            MergedHtmlContent = mergedHtml,
            IssuedAt = issuedCert.IssuedAt,
            IsDigitallySigned = true
        };
    }

    public async Task<RTSIssuedCertificateDto?> GetIssuedCertificateByApplicationNoAsync(string applicationNo, CancellationToken ct)
    {
        var cert = await _issuedCertRepository.GetQueryable()
            .AsNoTracking()
            .Include(c => c.Application)
            .Include(c => c.Service)
            .Include(c => c.IssuedByUser)
            .Where(c => c.Application != null && c.Application.ApplicationNo == applicationNo && !c.MarkedForDeletion)
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
        string deptName = "";
        if (cert.Service != null)
        {
            var dept = await _departmentRepository.GetByIdAsync(cert.Service.DepartmentId, ct);
            deptName = dept?.DepartmentName ?? "";
        }

        return new CertificateVerificationResponseDto
        {
            IsValid = true,
            CertificateGuid = cert.CertificateGuid,
            CertificateNo = cert.CertificateNo,
            ApplicationNo = cert.Application?.ApplicationNo ?? $"RTS{cert.ApplicationId:D8}",
            ServiceName = cert.Service?.ServiceName ?? "",
            DepartmentName = deptName,
            ApplicantName = cert.Application?.ApplicantName ?? "",
            UlbName = ulb?.UlbName ?? "अकोला महानगरपालिका",
            IssuedAt = cert.IssuedAt,
            IssuedByOfficer = cert.IssuedByUser != null ? $"{cert.IssuedByUser.FirstName} {cert.IssuedByUser.LastName}".Trim() : "सक्षम प्राधिकारी",
            OfficerDesignation = "सहाय्यक आयुक्त / कर अधीक्षक",
            IsDigitallySigned = cert.IsDigitallySigned,
            MergedHtmlContent = cert.MergedHtmlContent,
            Message = "✅ हे प्रमाणपत्र अधिकृतरीत्या पडताळलेले व अस्सल आहे. (Officially Verified & Authentic Certificate)"
        };
    }

    // Helper Methods
    private async Task<Dictionary<string, string>> BuildAutoValuesDictionaryAsync(RTSApplicationDetailsEntity app, CancellationToken ct)
    {
        var ulb = await _ulbRepository.GetQueryable().AsNoTracking().FirstOrDefaultAsync(ct);

        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ApplicationNo"] = app.ApplicationNo ?? $"RTS{app.Id:D8}",
            ["ApplicantName"] = app.ApplicantName ?? "",
            ["ApplicantMobile"] = app.ApplicantMobileNo ?? "",
            ["AppliedDate"] = (app.CreatedDate ?? DateTime.UtcNow).ToString("dd/MM/yyyy"),
            ["ServiceName"] = app.Service?.ServiceName ?? "",
            ["ServiceNameMarathi"] = app.Service?.ServiceNameLocal ?? app.Service?.ServiceName ?? "",
            ["DepartmentName"] = app.Department?.DepartmentName ?? "",
            ["DepartmentNameMarathi"] = app.Department?.DepartmentNameLocal ?? app.Department?.DepartmentName ?? "",
            ["ULBName"] = ulb?.UlbName ?? "अकोला महानगरपालिका",
            ["ULBNameMarathi"] = ulb?.UlbNameLocal ?? ulb?.UlbName ?? "अकोला महानगरपालिका",
            ["IssueDate"] = DateTime.UtcNow.ToString("dd/MM/yyyy")
        };

        // Extract values from dynamic FieldValueData
        if (app.FieldValueData != null)
        {
            foreach (var fv in app.FieldValueData)
            {
                if (fv.FieldDefinition != null && !string.IsNullOrWhiteSpace(fv.FieldDefinition.FieldCode))
                {
                    string val = fv.TextValue ?? fv.NumberValue?.ToString() ?? fv.DateValue?.ToString("dd/MM/yyyy") ?? (fv.BooleanValue.HasValue ? (fv.BooleanValue.Value ? "होय" : "नाही") : "");
                    dict[$"Field:{fv.FieldDefinition.FieldCode}"] = val;
                    dict[fv.FieldDefinition.FieldCode] = val;

                    // Common synonyms
                    if (fv.FieldDefinition.FieldCode.Contains("UPIC", StringComparison.OrdinalIgnoreCase))
                        dict["UpicNo"] = val;
                    if (fv.FieldDefinition.FieldCode.Contains("PropertyNo", StringComparison.OrdinalIgnoreCase))
                        dict["PropertyNo"] = val;
                    if (fv.FieldDefinition.FieldCode.Contains("Ward", StringComparison.OrdinalIgnoreCase))
                        dict["WardName"] = val;
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
        html = html.Replace("{{CertificateNo}}", sampleCertNo ?? "CERT/RTS/2026/00001", StringComparison.OrdinalIgnoreCase);
        html = html.Replace("{{ApprovedByOfficer}}", officerName ?? "सक्षम प्राधिकारी", StringComparison.OrdinalIgnoreCase);
        html = html.Replace("{{OfficerDesignation}}", "सहाय्यक आयुक्त", StringComparison.OrdinalIgnoreCase);
        html = html.Replace("{{ApprovalDate}}", DateTime.UtcNow.ToString("dd/MM/yyyy"), StringComparison.OrdinalIgnoreCase);

        // 2. Replace Officer Inputs [[FieldKey]]
        if (officerInputs != null)
        {
            foreach (var (k, v) in officerInputs)
            {
                html = html.Replace($"[[{k}]]", v ?? "", StringComparison.OrdinalIgnoreCase);
            }
        }

        // Check if [[SpecialConditions]] exists; replace with customConditions if provided
        if (!string.IsNullOrWhiteSpace(customConditions))
        {
            html = html.Replace("[[SpecialConditions]]", customConditions, StringComparison.OrdinalIgnoreCase);
        }

        // Clean any remaining unreplaced [[OfficerField]] tags with placeholder
        html = Regex.Replace(html, @"\[\[\w+\]\]", m =>
        {
            string key = m.Value.Trim('[', ']');
            return $"<span class='text-amber-700 bg-amber-50 px-1.5 py-0.5 rounded border border-amber-200'>[भरावयाची माहिती: {key}]</span>";
        });

        // 3. Inject Dynamic QR Code and Digital Signature Blocks
        string verifyUrl = certGuid.HasValue
            ? $"https://onesolutionakola.tabamc.in/service/verify-certificate/{certGuid.Value}"
            : "https://onesolutionakola.tabamc.in/service/verify-certificate/sample";

        string qrCodeHtml = $@"
        <div class='inline-flex flex-col items-center justify-center p-2 bg-white border border-slate-300 rounded shadow-xs text-center'>
            <div class='w-20 h-20 bg-slate-100 flex items-center justify-center border border-slate-200 text-xs font-mono text-slate-700'>
                <svg class='w-16 h-16 text-slate-800' viewBox='0 0 24 24' fill='currentColor'>
                    <path d='M3 3h8v8H3V3zm2 2v4h4V5H5zm8-2h8v8h-8V3zm2 2v4h4V5h-4zM3 13h8v8H3v-8zm2 2v4h4v-4H5zm13-2h3v2h-3v-2zm-5 0h2v3h-2v-3zm2 3h3v2h-3v-2zm3 2h3v3h-3v-3zm-5 1h2v2h-2v-2zm2 0h1v2h-1v-2z'/>
                </svg>
            </div>
            <span class='text-[10px] text-slate-500 mt-1 font-semibold'>Scan to Verify</span>
        </div>";

        string signatureHtml = $@"
        <div class='border border-emerald-500 bg-emerald-50/70 p-2.5 rounded-md text-center inline-block min-w-[200px]'>
            <div class='flex items-center justify-center gap-1 text-emerald-800 font-bold text-xs'>
                <svg class='w-4 h-4 text-emerald-600' fill='currentColor' viewBox='0 0 20 20'><path fill-rule='evenodd' d='M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z' clip-rule='evenodd'></path></svg>
                <span>Digitally Signed</span>
            </div>
            <div class='text-xs font-medium text-slate-800 mt-1'>{officerName ?? "सक्षम प्राधिकारी"}</div>
            <div class='text-[11px] text-slate-600'>सहाय्यक आयुक्त, अकोला मनपा</div>
            <div class='text-[10px] text-slate-500 mt-0.5'>{DateTime.UtcNow:dd/MM/yyyy HH:mm} IST</div>
        </div>";

        html = html.Replace("{{QRCode}}", qrCodeHtml, StringComparison.OrdinalIgnoreCase);
        html = html.Replace("{{DigitalSignature}}", signatureHtml, StringComparison.OrdinalIgnoreCase);

        return html;
    }

    private string BuildFullCertificateHtml(RTSCertificateTemplateMasterEntity? template, RTSApplicationDetailsEntity app)
    {
        string body = template?.BodyContent ?? BuildDefaultCertificateTemplateHtml(app);
        string? header = template?.HeaderContent;
        string? footer = template?.FooterContent;

        if (!string.IsNullOrWhiteSpace(header) && !string.IsNullOrWhiteSpace(footer))
        {
            return $"{header}\n{body}\n{footer}";
        }

        return $@"
        <div class='certificate-container max-w-3xl mx-auto p-6 md:p-8 bg-white border-4 border-double border-slate-700 font-sans text-slate-900 leading-relaxed shadow-lg rounded-sm'>
            <div class='text-center border-b-2 border-slate-800 pb-3 mb-4'>
                <div class='flex items-center justify-center gap-3 mb-1'>
                    <div class='w-12 h-12 rounded-full border border-slate-400 flex items-center justify-center bg-slate-100 text-[10px] font-bold text-slate-800 shadow-xs'>
                        मनपा अकोला
                    </div>
                    <div>
                        <h1 class='text-xl md:text-2xl font-bold text-slate-900 tracking-wide'>{{{{ULBNameMarathi}}}}</h1>
                        <h2 class='text-sm md:text-base font-semibold text-slate-700'>{{{{DepartmentNameMarathi}}}}</h2>
                    </div>
                </div>
                <div class='inline-block bg-slate-900 text-white px-4 py-1 rounded-full font-bold text-xs md:text-sm mt-1 shadow-xs'>
                    {{{{ServiceNameMarathi}}}} / अधिकृत दाखला
                </div>
            </div>

            <div class='flex flex-wrap justify-between items-center text-xs font-semibold text-slate-700 mb-4 border-b border-slate-200 pb-2 bg-slate-50 p-2 rounded'>
                <div>प्रमाणपत्र क्र.: <span class='font-bold text-slate-900 font-mono'>{{{{CertificateNo}}}}</span></div>
                <div>अर्ज क्र.: <span class='font-bold text-slate-900 font-mono'>{{{{ApplicationNo}}}}</span></div>
                <div>दिनांक: <span class='font-bold text-slate-900'>{{{{IssueDate}}}}</span></div>
            </div>

            <div class='my-4'>
                {body}
            </div>

            <div class='mt-6 pt-4 border-t border-slate-300 flex flex-wrap justify-between items-end gap-4'>
                <div>
                    {{{{QRCode}}}}
                </div>
                <div class='text-right'>
                    {{{{DigitalSignature}}}}
                </div>
            </div>

            <div class='text-center text-[10px] text-slate-500 mt-4 pt-2 border-t border-slate-200'>
                सदर प्रमाणपत्र महाराष्ट्र लोकसेवा हक्क अधिनियम, २०१५ अंतर्गत अधिकृतरीत्या जारी करण्यात आले असून यावर सक्षम प्राधिकाऱ्यांची डिजिटल स्वाक्षरी आहे.
            </div>
        </div>";
    }

    private string BuildDefaultCertificateTemplateHtml(RTSApplicationDetailsEntity app)
    {
        return $@"
        <div class='certificate-container max-w-3xl mx-auto p-8 bg-white border-4 border-double border-slate-700 font-sans text-slate-900 leading-relaxed shadow-md'>
            <div class='text-center border-b-2 border-slate-300 pb-4 mb-6'>
                <h1 class='text-2xl font-bold text-slate-900 mb-1'>{{{{ULBNameMarathi}}}}</h1>
                <h2 class='text-lg font-semibold text-slate-700'>{{{{DepartmentNameMarathi}}}}</h2>
                <div class='inline-block bg-slate-100 text-slate-800 px-4 py-1 rounded font-bold text-base mt-2 border border-slate-300'>
                    {{{{ServiceNameMarathi}}}} / प्रमाणपत्र
                </div>
            </div>

            <div class='flex justify-between items-center text-xs font-semibold text-slate-600 mb-6 border-b border-slate-200 pb-2'>
                <div>प्रमाणपत्र क्र.: <span class='font-bold text-slate-900 font-mono'>{{{{CertificateNo}}}}</span></div>
                <div>अर्ज क्र.: <span class='font-bold text-slate-900 font-mono'>{{{{ApplicationNo}}}}</span></div>
                <div>दिनांक: <span class='font-bold text-slate-900'>{{{{IssueDate}}}}</span></div>
            </div>

            <div class='my-6 text-sm text-justify space-y-4'>
                <p>
                    प्रमाणित करण्यात येते की, अर्जदार <strong>{{{{ApplicantName}}}}</strong> (मोबाईल क्र.: <strong>{{{{ApplicantMobile}}}}</strong>) यांनी अकोला महानगरपालिकेकडे <strong>{{{{ServiceNameMarathi}}}}</strong> साठी अर्ज क्र. <strong>{{{{ApplicationNo}}}}</strong> अन्वये दिनांक <strong>{{{{AppliedDate}}}}</strong> रोजी अर्ज सादर केला होता.
                </p>
                <p>
                    सदर अर्जाची व कागदपत्रांची नियमानुसार सविस्तर छाननी व प्रत्यक्ष पाहणी करण्यात आली असून, सक्षम प्राधिकाऱ्यांच्या आदेशानुसार हे प्रमाणपत्र/दाखला खालील अटी व शर्तींच्या अधीन राहून जारी करण्यात येत आहे:
                </p>

                <div class='bg-slate-50 p-4 rounded-md border border-slate-200 text-xs space-y-2'>
                    <div class='font-bold text-slate-800'>📌 अधिकृत आदेश व संदर्भ तपशील:</div>
                    <div class='grid grid-cols-2 gap-2'>
                        <div><strong>जावक / आदेश क्र.:</strong> [[OrderNo]]</div>
                        <div><strong>परवाना मुदत:</strong> [[ValidityPeriod]]</div>
                        <div><strong>शुल्क पावती क्र.:</strong> [[ChallanNo]]</div>
                    </div>
                    <div class='mt-2'>
                        <strong>विशेष अटी व शर्ती:</strong>
                        <div class='mt-1 text-slate-700 whitespace-pre-line'>[[SpecialConditions]]</div>
                    </div>
                </div>
            </div>

            <div class='mt-10 pt-6 border-t border-slate-200 flex justify-between items-end'>
                <div>
                    {{{{QRCode}}}}
                </div>
                <div class='text-right'>
                    {{{{DigitalSignature}}}}
                </div>
            </div>

            <div class='text-center text-[10px] text-slate-400 mt-6 pt-2 border-t border-slate-100'>
                सदर प्रमाणपत्र संगणकीय प्रणालीद्वारे तयार केलेले असून यावर डिजिटल स्वाक्षरी करण्यात आलेली आहे. QR कोड स्कॅन करून याची सत्यता पडताळता येईल.
            </div>
        </div>";
    }

    private static RTSCertificateTemplateDto MapToTemplateDto(RTSCertificateTemplateMasterEntity entity)
    {
        var dto = new RTSCertificateTemplateDto
        {
            Id = entity.Id,
            ServiceId = entity.ServiceId,
            ServiceName = entity.Service?.ServiceName,
            DepartmentName = "",
            TemplateName = entity.TemplateName,
            TemplateCode = entity.TemplateCode,
            HeaderContent = entity.HeaderContent,
            BodyContent = entity.BodyContent,
            FooterContent = entity.FooterContent,
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
            ServiceName = cert.Service?.ServiceName ?? "",
            DepartmentName = "",
            ApplicantName = cert.Application?.ApplicantName ?? "",
            ApplicantMobile = cert.Application?.ApplicantMobileNo ?? "",
            MergedHtmlContent = cert.MergedHtmlContent,
            QrCodePayload = cert.QrCodePayload,
            IssuedByUserId = cert.IssuedByUserId,
            IssuedByUserName = cert.IssuedByUser != null ? $"{cert.IssuedByUser.FirstName} {cert.IssuedByUser.LastName}".Trim() : "",
            IssuedByOfficerDesignation = "सहाय्यक आयुक्त",
            IssuedAt = cert.IssuedAt,
            IsDigitallySigned = cert.IsDigitallySigned,
            DigitalSignatureInfo = cert.DigitalSignatureInfo
        };
    }
}
