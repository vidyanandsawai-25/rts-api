using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Asset_Management.SubUnitsDetails;

/// <summary>
/// Response DTO for GetByAssetId that includes floor details and total base value.
/// </summary>
public class SubUnitsDetailsSummaryDto
{
    public List<SubUnitsDetailsDto> FloorDetails { get; set; } = new();
    public decimal TotalBaseValue { get; set; }
    public decimal TotalCapitalValue { get; set; }
    public decimal TotalMarketValue { get; set; }
    public int TotalFloors { get; set; }
}

/// <summary>
/// Display names resolved by joining a sub-unit detail's FK ids against their master tables
/// (AssetMaster/FloorMaster/SubFloorMaster/ConstructionTypeMaster/AssetTypeOfUseMaster/
/// AssetSubTypeOfUseMaster). Not backed by columns on AMS.SubUnitsDetails itself.
/// </summary>
public class SubUnitsDetailsNamesDto
{
    public string? AssetName { get; set; }
    public string? FloorName { get; set; }
    public string? SubFloorName { get; set; }
    public string? ConstructionTypeName { get; set; }
    public string? TypeOfUseName { get; set; }
    public string? SubTypeOfUseName { get; set; }
}

public class SubUnitsDetailsDto : BaseDtos
{
    public int AssetId { get; set; }
    public int FloorId { get; set; }
    public int? SubFloorId { get; set; }
    public string? ConstructionYear { get; set; }
    public string? AssessmentYear { get; set; }
    public int ConstructionTypeId { get; set; }
    public int TypeOfUseId { get; set; }
    public int? SubTypeOfUseId { get; set; }
    public decimal? CarpetAreaSqMeter { get; set; }
    public decimal? CarpetAreaSqFeet { get; set; }
    public decimal? BuiltUpAreaSqMeter { get; set; }
    public decimal? BuiltUpAreaSqFeet { get; set; }
    public int? NoOfRooms { get; set; }
    public int SubAssetCount { get; set; }
    public decimal? CapitalValue { get; set; }
    public decimal? BaseValue { get; set; }
    public decimal? CVBaseRate { get; set; }
    public decimal? CVAgeFactor { get; set; }
    public decimal? CVFloorFactor { get; set; }
    public decimal? CVNatureFactor { get; set; }
    public decimal? CVUseFactor { get; set; }
    public bool? IsRented { get; set; }
    public string? CVCalculationFormula { get; set; }
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }

    public SubUnitsDetailsNamesDto Names { get; set; } = new();

    public List<NtisPlatform.Application.DTOs.Asset_Management.AssetMaster.RoomDetailDto>? RoomDetails { get; set; }
}

public class CreateSubUnitsDetailsDto : CreateBaseDtos
{
    [Required(ErrorMessage = "AMS_SubUnitsDetails_AssetId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_SubUnitsDetails_AssetId_InvalidRange")]
    public int AssetId { get; set; }

    [Required(ErrorMessage = "AMS_SubUnitsDetails_FloorId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_SubUnitsDetails_FloorId_InvalidRange")]
    public int FloorId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_SubUnitsDetails_SubFloorId_InvalidRange")]
    public int? SubFloorId { get; set; }

    [StringLength(4, ErrorMessage = "AMS_SubUnitsDetails_ConstructionYear_MaxLengthExceeded_4")]
    [RegularExpression(@"^\d{4}$", ErrorMessage = "AMS_SubUnitsDetails_ConstructionYear_Invalid")]
    public string? ConstructionYear { get; set; }

    [StringLength(4, ErrorMessage = "AMS_SubUnitsDetails_AssessmentYear_MaxLengthExceeded_4")]
    [RegularExpression(@"^\d{4}$", ErrorMessage = "AMS_SubUnitsDetails_AssessmentYear_Invalid")]
    public string? AssessmentYear { get; set; }

    [Required(ErrorMessage = "AMS_SubUnitsDetails_ConstructionTypeId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_SubUnitsDetails_ConstructionTypeId_InvalidRange")]
    public int ConstructionTypeId { get; set; }

    [Required(ErrorMessage = "AMS_SubUnitsDetails_TypeOfUseId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_SubUnitsDetails_TypeOfUseId_InvalidRange")]
    public int TypeOfUseId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_SubUnitsDetails_SubTypeOfUseId_InvalidRange")]
    public int? SubTypeOfUseId { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_SubUnitsDetails_CarpetAreaSqMeter_InvalidRange")]
    public decimal? CarpetAreaSqMeter { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_SubUnitsDetails_CarpetAreaSqFeet_InvalidRange")]
    public decimal? CarpetAreaSqFeet { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_SubUnitsDetails_BuiltUpAreaSqMeter_InvalidRange")]
    public decimal? BuiltUpAreaSqMeter { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_SubUnitsDetails_BuiltUpAreaSqFeet_InvalidRange")]
    public decimal? BuiltUpAreaSqFeet { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "AMS_SubUnitsDetails_NoOfRooms_InvalidRange")]
    public int? NoOfRooms { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_SubUnitsDetails_CapitalValue_InvalidRange")]
    public decimal? CapitalValue { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_SubUnitsDetails_BaseValue_InvalidRange")]
    public decimal? BaseValue { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_SubUnitsDetails_CVBaseRate_InvalidRange")]
    public decimal? CVBaseRate { get; set; }

    [Range(0, 9.9999, ErrorMessage = "AMS_SubUnitsDetails_CVAgeFactor_InvalidRange")]
    public decimal? CVAgeFactor { get; set; }

    [Range(0, 9.9999, ErrorMessage = "AMS_SubUnitsDetails_CVFloorFactor_InvalidRange")]
    public decimal? CVFloorFactor { get; set; }

    [Range(0, 9.9999, ErrorMessage = "AMS_SubUnitsDetails_CVNatureFactor_InvalidRange")]
    public decimal? CVNatureFactor { get; set; }

    [Range(0, 9.9999, ErrorMessage = "AMS_SubUnitsDetails_CVUseFactor_InvalidRange")]
    public decimal? CVUseFactor { get; set; }

    [StringLength(500, ErrorMessage = "AMS_SubUnitsDetails_CVCalculationFormula_MaxLengthExceeded_500")]
    public string? CVCalculationFormula { get; set; }

    public List<NtisPlatform.Application.DTOs.Asset_Management.AssetMaster.RoomDetailDto>? RoomDetails { get; set; }
}

public class UpdateSubUnitsDetailsDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "AMS_SubUnitsDetails_AssetId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_SubUnitsDetails_AssetId_InvalidRange")]
    public int AssetId { get; set; }

    [Required(ErrorMessage = "AMS_SubUnitsDetails_FloorId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_SubUnitsDetails_FloorId_InvalidRange")]
    public int FloorId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_SubUnitsDetails_SubFloorId_InvalidRange")]
    public int? SubFloorId { get; set; }

    [StringLength(4, ErrorMessage = "AMS_SubUnitsDetails_ConstructionYear_MaxLengthExceeded_4")]
    [RegularExpression(@"^\d{4}$", ErrorMessage = "AMS_SubUnitsDetails_ConstructionYear_Invalid")]
    public string? ConstructionYear { get; set; }

    [StringLength(4, ErrorMessage = "AMS_SubUnitsDetails_AssessmentYear_MaxLengthExceeded_4")]
    [RegularExpression(@"^\d{4}$", ErrorMessage = "AMS_SubUnitsDetails_AssessmentYear_Invalid")]
    public string? AssessmentYear { get; set; }

    [Required(ErrorMessage = "AMS_SubUnitsDetails_ConstructionTypeId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_SubUnitsDetails_ConstructionTypeId_InvalidRange")]
    public int ConstructionTypeId { get; set; }

    [Required(ErrorMessage = "AMS_SubUnitsDetails_TypeOfUseId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_SubUnitsDetails_TypeOfUseId_InvalidRange")]
    public int TypeOfUseId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_SubUnitsDetails_SubTypeOfUseId_InvalidRange")]
    public int? SubTypeOfUseId { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_SubUnitsDetails_CarpetAreaSqMeter_InvalidRange")]
    public decimal? CarpetAreaSqMeter { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_SubUnitsDetails_CarpetAreaSqFeet_InvalidRange")]
    public decimal? CarpetAreaSqFeet { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_SubUnitsDetails_BuiltUpAreaSqMeter_InvalidRange")]
    public decimal? BuiltUpAreaSqMeter { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_SubUnitsDetails_BuiltUpAreaSqFeet_InvalidRange")]
    public decimal? BuiltUpAreaSqFeet { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "AMS_SubUnitsDetails_NoOfRooms_InvalidRange")]
    public int? NoOfRooms { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_SubUnitsDetails_CapitalValue_InvalidRange")]
    public decimal? CapitalValue { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_SubUnitsDetails_BaseValue_InvalidRange")]
    public decimal? BaseValue { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_SubUnitsDetails_CVBaseRate_InvalidRange")]
    public decimal? CVBaseRate { get; set; }

    [Range(0, 9.9999, ErrorMessage = "AMS_SubUnitsDetails_CVAgeFactor_InvalidRange")]
    public decimal? CVAgeFactor { get; set; }

    [Range(0, 9.9999, ErrorMessage = "AMS_SubUnitsDetails_CVFloorFactor_InvalidRange")]
    public decimal? CVFloorFactor { get; set; }

    [Range(0, 9.9999, ErrorMessage = "AMS_SubUnitsDetails_CVNatureFactor_InvalidRange")]
    public decimal? CVNatureFactor { get; set; }

    [Range(0, 9.9999, ErrorMessage = "AMS_SubUnitsDetails_CVUseFactor_InvalidRange")]
    public decimal? CVUseFactor { get; set; }

    [StringLength(500, ErrorMessage = "AMS_SubUnitsDetails_CVCalculationFormula_MaxLengthExceeded_500")]
    public string? CVCalculationFormula { get; set; }

    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}
