using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Asset_Management.AssetUseFactorCVMaster;

/// <summary>Read model for [AMS].[UseFactorCVMaster].</summary>
public class AssetUseFactorCVMasterDto : BaseDtos
{
    public int TypeOfUseId { get; set; }
    public string TypeOfUseDescription { get; set; } = string.Empty;
    public int SubTypeOfUseId { get; set; }
    public string SubTypeOfUseDescription { get; set; } = string.Empty;
    public decimal Factor { get; set; }
    public int YearRangeCVId { get; set; }
}

public class CreateAssetUseFactorCVMasterDto : CreateBaseDtos
{
    [Required(ErrorMessage = "AssetUseFactorCV_TypeOfUseId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "UseFactorCV_TypeOfUseId_Invalid")]
    public int? TypeOfUseId { get; set; }

    [Required(ErrorMessage = "AssetUseFactorCV_SubTypeOfUseId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "UseFactorCV_SubTypeOfUseId_Invalid")]
    public int? SubTypeOfUseId { get; set; }

    [Required(ErrorMessage = "AssetUseFactorCV_Factor_Required")]
    [Range(typeof(decimal), "0.01", "999.99", ErrorMessage = "UseFactorCV_Factor_Range")]
    public decimal Factor { get; set; }

    [Required(ErrorMessage = "AssetUseFactorCV_YearRangeCVId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "UseFactorCV_YearRangeCVId_Invalid")]
    public int? YearRangeCVId { get; set; }
}

public class UpdateAssetUseFactorCVMasterDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "AssetUseFactorCV_TypeOfUseId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "UseFactorCV_TypeOfUseId_Invalid")]
    public int? TypeOfUseId { get; set; }

    [Required(ErrorMessage = "AssetUseFactorCV_SubTypeOfUseId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "UseFactorCV_SubTypeOfUseId_Invalid")]
    public int? SubTypeOfUseId { get; set; }

    [Required(ErrorMessage = "AssetUseFactorCV_Factor_Required")]
    [Range(typeof(decimal), "0.01", "999.99", ErrorMessage = "UseFactorCV_Factor_Range")]
    public decimal Factor { get; set; }

    [Required(ErrorMessage = "AssetUseFactorCV_YearRangeCVId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "UseFactorCV_YearRangeCVId_Invalid")]
    public int? YearRangeCVId { get; set; }
}

