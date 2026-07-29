namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// Represents a certificate tax guideline entity (PTIS.CertificateTaxGuideline table).
/// </summary>
public class CertificateTaxGuidelineEntity : BaseEntity
{
    public string GuidelineCode { get; set; } = string.Empty;
    public string GuidelineName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string GuidelineGroup { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string DataType { get; set; } = string.Empty;
    public string? GuidelineValue { get; set; }
    public string? AllowedValues { get; set; }
}
