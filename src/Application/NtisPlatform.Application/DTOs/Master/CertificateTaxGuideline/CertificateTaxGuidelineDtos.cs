using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.CertificateTaxGuideline;

public class CertificateTaxGuidelineDto : BaseDtos
{
    public string GuidelineCode { get; set; } = string.Empty;
    public string GuidelineName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? GuidelineGroup { get; set; }
    public int? DisplayOrder { get; set; }
    public string DataType { get; set; } = string.Empty;
    public string? GuidelineValue { get; set; }
    public string? AllowedValues { get; set; }
}

public class CreateCertificateTaxGuidelineDto : CreateBaseDtos
{
    [Required(ErrorMessage = "CertificateTaxGuideline_GuidelineCode_Required")]
    [StringLength(50, ErrorMessage = "CertificateTaxGuideline_GuidelineCode_MaxLen_50")]
    public string GuidelineCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "CertificateTaxGuideline_GuidelineName_Required")]
    [StringLength(150, ErrorMessage = "CertificateTaxGuideline_GuidelineName_MaxLen_150")]
    public string GuidelineName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "CertificateTaxGuideline_Description_MaxLen_500")]
    public string? Description { get; set; }

    [StringLength(50, ErrorMessage = "CertificateTaxGuideline_GuidelineGroup_MaxLen_50")]
    public string? GuidelineGroup { get; set; }

    public int? DisplayOrder { get; set; }

    [Required(ErrorMessage = "CertificateTaxGuideline_DataType_Required")]
    [StringLength(20, ErrorMessage = "CertificateTaxGuideline_DataType_MaxLen_20")]
    [RegularExpression("^(BIT|INT|DECIMAL|VARCHAR)$", ErrorMessage = "CertificateTaxGuideline_DataType_Invalid")]
    public string DataType { get; set; } = "VARCHAR";

    [StringLength(500, ErrorMessage = "CertificateTaxGuideline_GuidelineValue_MaxLen_500")]
    public string? GuidelineValue { get; set; }

    [StringLength(500, ErrorMessage = "CertificateTaxGuideline_AllowedValues_MaxLen_500")]
    public string? AllowedValues { get; set; }
}

public class UpdateCertificateTaxGuidelineDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "CertificateTaxGuideline_GuidelineCode_Required")]
    [StringLength(50, ErrorMessage = "CertificateTaxGuideline_GuidelineCode_MaxLen_50")]
    public string GuidelineCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "CertificateTaxGuideline_GuidelineName_Required")]
    [StringLength(150, ErrorMessage = "CertificateTaxGuideline_GuidelineName_MaxLen_150")]
    public string GuidelineName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "CertificateTaxGuideline_Description_MaxLen_500")]
    public string? Description { get; set; }

    [StringLength(50, ErrorMessage = "CertificateTaxGuideline_GuidelineGroup_MaxLen_50")]
    public string? GuidelineGroup { get; set; }

    public int? DisplayOrder { get; set; }

    [Required(ErrorMessage = "CertificateTaxGuideline_DataType_Required")]
    [StringLength(20, ErrorMessage = "CertificateTaxGuideline_DataType_MaxLen_20")]
    [RegularExpression("^(BIT|INT|DECIMAL|VARCHAR)$", ErrorMessage = "CertificateTaxGuideline_DataType_Invalid")]
    public string DataType { get; set; } = "VARCHAR";

    [StringLength(500, ErrorMessage = "CertificateTaxGuideline_GuidelineValue_MaxLen_500")]
    public string? GuidelineValue { get; set; }

    [StringLength(500, ErrorMessage = "CertificateTaxGuideline_AllowedValues_MaxLen_500")]
    public string? AllowedValues { get; set; }
}

