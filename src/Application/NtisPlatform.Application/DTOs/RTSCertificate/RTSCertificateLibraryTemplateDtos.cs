using System.Text.Json.Serialization;

namespace NtisPlatform.Application.DTOs.RTSCertificate;

public class RTSCertificateLibraryTemplateDto
{
    public int Id { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public string TemplateCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? HeaderContent { get; set; }
    public string BodyContent { get; set; } = string.Empty;
    public string? FooterContent { get; set; }
    public string? DesignJson { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}

public class CreateRTSCertificateLibraryTemplateDto
{
    private string? _designJson;

    public string TemplateName { get; set; } = string.Empty;
    public string TemplateCode { get; set; } = string.Empty;
    public string? Description { get; set; }
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

    public bool IsActive { get; set; } = true;
}

public class UpdateRTSCertificateLibraryTemplateDto : CreateRTSCertificateLibraryTemplateDto
{
    public int Id { get; set; }
}
