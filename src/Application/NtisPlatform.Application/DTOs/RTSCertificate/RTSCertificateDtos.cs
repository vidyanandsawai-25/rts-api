using System.Text.Json.Serialization;

namespace NtisPlatform.Application.DTOs.RTSCertificate;

public class OfficerFieldConfigDto
{
    public string FieldKey { get; set; } = string.Empty;
    public string FieldLabelMarathi { get; set; } = string.Empty;
    public string FieldLabelEnglish { get; set; } = string.Empty;
    public string FieldType { get; set; } = "text"; // "text" | "textarea" | "number" | "date" | "select"
    public bool IsMandatory { get; set; } = true;
    public string? DefaultValue { get; set; }
    public List<string>? Options { get; set; }
}

public class RTSCertificateTemplateDto
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public string? ServiceName { get; set; }
    public string? DepartmentName { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public string TemplateCode { get; set; } = string.Empty;
    public string? HeaderContent { get; set; }
    public string BodyContent { get; set; } = string.Empty;
    public string? FooterContent { get; set; }
    public string? DesignJson { get; set; }
    public string? DefaultConditionsJson { get; set; }
    public string? OfficerFieldsConfigJson { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }

    public List<OfficerFieldConfigDto> OfficerFields { get; set; } = new();
    public List<string> DefaultConditions { get; set; } = new();
}

public class CreateRTSCertificateTemplateDto
{
    private string? _designJson;

    public int ServiceId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public string TemplateCode { get; set; } = string.Empty;
    public string? HeaderContent { get; set; }
    public string BodyContent { get; set; } = string.Empty;
    public string? FooterContent { get; set; }
    public string? DesignJson
    {
        get => _designJson;
        set
        {
            _designJson = value;
            DesignJsonSpecified = true;
        }
    }

    [JsonIgnore]
    public bool DesignJsonSpecified { get; private set; }

    public string? DefaultConditionsJson { get; set; }
    public string? OfficerFieldsConfigJson { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateRTSCertificateTemplateDto : CreateRTSCertificateTemplateDto
{
    public int Id { get; set; }
}

public class CertificatePreviewRequestDto
{
    public int ApplicationId { get; set; }
    public Dictionary<string, string> OfficerInputs { get; set; } = new();
    public string? CustomConditions { get; set; }
}

public class CertificatePreviewResponseDto
{
    public bool HasTemplate { get; set; }
    public int TemplateId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public string MergedHtml { get; set; } = string.Empty;
    public Dictionary<string, string> CitizenAutoValues { get; set; } = new();
    public List<OfficerFieldConfigDto> RequiredOfficerFields { get; set; } = new();
    public List<string> DefaultConditions { get; set; } = new();
    public string? SampleCertificateNo { get; set; }
}

public class IssueCertificateRequestDto
{
    public int ApplicationId { get; set; }
    public Dictionary<string, string> OfficerInputs { get; set; } = new();
    public string? CustomConditions { get; set; }
    public string? ActionRemark { get; set; }
    public bool SignAndApprove { get; set; } = true;
}

public class RTSIssuedCertificateDto
{
    public int Id { get; set; }
    public Guid CertificateGuid { get; set; }
    public string CertificateNo { get; set; } = string.Empty;
    public int ApplicationId { get; set; }
    public string ApplicationNo { get; set; } = string.Empty;
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string ApplicantName { get; set; } = string.Empty;
    public string ApplicantMobile { get; set; } = string.Empty;
    public Dictionary<string, string> OfficerInputs { get; set; } = new();
    public string MergedHtmlContent { get; set; } = string.Empty;
    public string? QrCodePayload { get; set; }
    public int IssuedByUserId { get; set; }
    public string? IssuedByUserName { get; set; }
    public string? IssuedByOfficerDesignation { get; set; }
    public DateTime IssuedAt { get; set; }
    public bool IsDigitallySigned { get; set; }
    public string? DigitalSignatureInfo { get; set; }
}

public class CertificateVerificationResponseDto
{
    public bool IsValid { get; set; }
    public string? Message { get; set; }
    public Guid CertificateGuid { get; set; }
    public string? CertificateNo { get; set; }
    public string? ApplicationNo { get; set; }
    public string? ServiceName { get; set; }
    public string? DepartmentName { get; set; }
    public string? ApplicantName { get; set; }
    public string? UlbName { get; set; }
    public string? UlbLogo { get; set; }
    public string? UlbAddress { get; set; }
    public DateTime? IssuedAt { get; set; }
    public string? IssuedByOfficer { get; set; }
    public string? OfficerDesignation { get; set; }
    public bool IsDigitallySigned { get; set; }
    public string? DigitalSignatureInfo { get; set; }
    public string? DscSignerName { get; set; }
    public string? DscIssuer { get; set; }
    public string? DscSerialNumber { get; set; }
    public string? DscThumbprint { get; set; }
    public DateTime? DscValidUntil { get; set; }
    public string? MergedHtmlContent { get; set; }
}

public class CertificateAvailableTagDto
{
    public string TagKey { get; set; } = string.Empty; // e.g. "{{ApplicantName}}"
    public string TagLabelMarathi { get; set; } = string.Empty;
    public string TagLabelEnglish { get; set; } = string.Empty;
    public string SourceType { get; set; } = "Citizen"; // "Citizen" | "System" | "Officer"
}
