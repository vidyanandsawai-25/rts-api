using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Master;

/// <summary>Read model for a GST/tax rate.</summary>
public class GSTDto : BaseDtos
{
    public string TaxCode { get; set; } = string.Empty;
    public string TaxName { get; set; } = string.Empty;
    public decimal TaxPercentage { get; set; }
    public DateTime EffectiveFromDate { get; set; }
    public DateTime? EffectiveToDate { get; set; }
}

public class CreateGSTDto : CreateBaseDtos
{
    [Required(ErrorMessage = "GST_TaxCode_Required")]
    [StringLength(50, ErrorMessage = "GST_TaxCode_MaxLen_50")]
    public string TaxCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "GST_TaxName_Required")]
    [StringLength(100, ErrorMessage = "GST_TaxName_MaxLen_100")]
    public string TaxName { get; set; } = string.Empty;

    [Range(0, 100, ErrorMessage = "GST_TaxPercentage_Range")]
    public decimal TaxPercentage { get; set; }

    [Required(ErrorMessage = "GST_EffectiveFromDate_Required")]
    public DateTime EffectiveFromDate { get; set; }

    public DateTime? EffectiveToDate { get; set; }
}

public class UpdateGSTDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "GST_TaxCode_Required")]
    [StringLength(50, ErrorMessage = "GST_TaxCode_MaxLen_50")]
    public string TaxCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "GST_TaxName_Required")]
    [StringLength(100, ErrorMessage = "GST_TaxName_MaxLen_100")]
    public string TaxName { get; set; } = string.Empty;

    [Range(0, 100, ErrorMessage = "GST_TaxPercentage_Range")]
    public decimal TaxPercentage { get; set; }

    [Required(ErrorMessage = "GST_EffectiveFromDate_Required")]
    public DateTime EffectiveFromDate { get; set; }

    public DateTime? EffectiveToDate { get; set; }
}
