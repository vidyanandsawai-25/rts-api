namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// Represents a certificate tax guideline entity (PTIS.CertificateTaxGuideline table).
/// </summary>
public class CertificateTaxGuidelineEntity : BaseEntity
{
    public string? GuidelineCode { get; set; }
    public string? GuidelineName { get; set; }
    public string? Description { get; set; }
    public string? GuidelineGroup { get; set; }
    public int DisplayOrder { get; set; }
    public string? DataType { get; set; }
    public string? GuidelineValue { get; set; }
    public string? AllowedValues { get; set; }
}
