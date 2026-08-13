using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Asset_Management.AssetNatureFactorCVMaster;

/// <summary>Read model for [AMS].[NatureFactorCVMaster].</summary>
public class AssetNatureFactorCVMasterDto : BaseDtos
{
    public int ConstructionTypeId { get; set; }
    public string ConstructionTypeDescription { get; set; } = string.Empty;
    public decimal Factor { get; set; }
    public int YearRangeCVId { get; set; }
}

public class CreateAssetNatureFactorCVMasterDto : CreateBaseDtos
{
    [Required(ErrorMessage = "NatureFactorCV_ConstructionTypeId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "NatureFactorCV_ConstructionTypeId_Invalid")]
    public int? ConstructionTypeId { get; set; }

    [Required(ErrorMessage = "NatureFactorCV_Factor_Required")]
    [Range(typeof(decimal), "0.01", "999.99", ErrorMessage = "NatureFactorCV_Factor_Range")]
    public decimal Factor { get; set; }

    [Required(ErrorMessage = "NatureFactorCV_YearRangeCVId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "NatureFactorCV_YearRangeCVId_Invalid")]
    public int? YearRangeCVId { get; set; }
}

public class UpdateAssetNatureFactorCVMasterDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "NatureFactorCV_ConstructionTypeId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "NatureFactorCV_ConstructionTypeId_Invalid")]
    public int? ConstructionTypeId { get; set; }

    [Required(ErrorMessage = "NatureFactorCV_Factor_Required")]
    [Range(typeof(decimal), "0.01", "999.99", ErrorMessage = "NatureFactorCV_Factor_Range")]
    public decimal Factor { get; set; }

    [Required(ErrorMessage = "NatureFactorCV_YearRangeCVId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "NatureFactorCV_YearRangeCVId_Invalid")]
    public int? YearRangeCVId { get; set; }
}