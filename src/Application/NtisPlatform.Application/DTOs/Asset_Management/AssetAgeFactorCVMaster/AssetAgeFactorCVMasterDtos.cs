using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Asset_Management.AssetAgeFactorCVMaster;

/// <summary>Read model for [AMS].[AgeFactorCVMaster].</summary>
public class AssetAgeFactorCVMasterDto : BaseDtos
{
    public int ConstructionTypeId { get; set; }
    public int AgeFrom { get; set; }
    public int AgeTo { get; set; }
    public decimal Factor { get; set; }
    public int YearRangeCVId { get; set; }
}

public class CreateAssetAgeFactorCVMasterDto : CreateBaseDtos
{
    [Required(ErrorMessage = "AgeFactorCV_ConstructionTypeId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AgeFactorCV_ConstructionTypeId_Invalid")]
    public int? ConstructionTypeId { get; set; }

    [Required(ErrorMessage = "AgeFactorCV_AgeFrom_Required")]
    [Range(0, int.MaxValue, ErrorMessage = "AgeFactorCV_AgeFrom_Invalid")]
    public int? AgeFrom { get; set; }

    [Required(ErrorMessage = "AgeFactorCV_AgeTo_Required")]
    [Range(0, int.MaxValue, ErrorMessage = "AgeFactorCV_AgeTo_Invalid")]
    public int? AgeTo { get; set; }

    [Required(ErrorMessage = "AgeFactorCV_Factor_Required")]
    [Range(typeof(decimal), "0.01", "999.99", ErrorMessage = "AgeFactorCV_Factor_Range")]
    public decimal Factor { get; set; }

    [Required(ErrorMessage = "AgeFactorCV_YearRangeCVId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AgeFactorCV_YearRangeCVId_Invalid")]
    public int? YearRangeCVId { get; set; }
}

public class UpdateAssetAgeFactorCVMasterDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "AgeFactorCV_ConstructionTypeId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AgeFactorCV_ConstructionTypeId_Invalid")]
    public int? ConstructionTypeId { get; set; }

    [Required(ErrorMessage = "AgeFactorCV_AgeFrom_Required")]
    [Range(0, int.MaxValue, ErrorMessage = "AgeFactorCV_AgeFrom_Invalid")]
    public int? AgeFrom { get; set; }

    [Required(ErrorMessage = "AgeFactorCV_AgeTo_Required")]
    [Range(0, int.MaxValue, ErrorMessage = "AgeFactorCV_AgeTo_Invalid")]
    public int? AgeTo { get; set; }

    [Required(ErrorMessage = "AgeFactorCV_Factor_Required")]
    [Range(typeof(decimal), "0.01", "999.99", ErrorMessage = "AgeFactorCV_Factor_Range")]
    public decimal Factor { get; set; }

    [Required(ErrorMessage = "AgeFactorCV_YearRangeCVId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AgeFactorCV_YearRangeCVId_Invalid")]
    public int? YearRangeCVId { get; set; }
}

/// <summary>Query parameters for the AMS age factor CV master listing.</summary>
public class AssetAgeFactorCVMasterQueryParameters : BaseQueryParameters
{
    [Filterable(FilterOperator.Equals)] [Sortable]
    public int? ConstructionTypeId { get; set; }

    [Filterable(FilterOperator.Equals)] [Sortable]
    public int? YearRangeCVId { get; set; }

    [Filterable(FilterOperator.Equals)] [Sortable]
    public bool? IsActive { get; set; }
}
