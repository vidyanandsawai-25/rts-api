using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Asset_Management.AssetAssessmentYearRangeMasterCV;

/// <summary>Read model for [AMS].[AssessmentYearRangeMaster].</summary>
public class AssetAssessmentYearRangeMasterCVDto : BaseDtos
{
    public int FromYear { get; set; }
    public int ToYear { get; set; }
}

public class CreateAssetAssessmentYearRangeMasterCVDto : CreateBaseDtos
{
    [Required(ErrorMessage = "AssessmentYearRangeCV_FromYear_Required")]
    [Range(1900, 9999, ErrorMessage = "AssessmentYearRangeCV_FromYear_Invalid")]
    public int? FromYear { get; set; }

    [Required(ErrorMessage = "AssessmentYearRangeCV_ToYear_Required")]
    [Range(1900, 9999, ErrorMessage = "AssessmentYearRangeCV_ToYear_Invalid")]
    public int? ToYear { get; set; }
}

public class UpdateAssetAssessmentYearRangeMasterCVDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "AssessmentYearRangeCV_FromYear_Required")]
    [Range(1900, 9999, ErrorMessage = "AssessmentYearRangeCV_FromYear_Invalid")]
    public int? FromYear { get; set; }

    [Required(ErrorMessage = "AssessmentYearRangeCV_ToYear_Required")]
    [Range(1900, 9999, ErrorMessage = "AssessmentYearRangeCV_ToYear_Invalid")]
    public int? ToYear { get; set; }
}

/// <summary>Query parameters for the AMS assessment year range CV master listing.</summary>
public class AssetAssessmentYearRangeMasterCVQueryParameters : BaseQueryParameters
{
    [Filterable(FilterOperator.Equals)] [Sortable]
    public int? FromYear { get; set; }

    [Filterable(FilterOperator.Equals)] [Sortable]
    public int? ToYear { get; set; }

    [Filterable(FilterOperator.Equals)] [Sortable]
    public bool? IsActive { get; set; }
}
